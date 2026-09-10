namespace De.Hochstaetter.Fronius.Services;

public partial class WattPilotService : BindableBase, IWattPilotService
{
    private readonly List<WattPilotAcknowledge> outstandingAcknowledges = [];
    private readonly byte[] buffer = new byte[8192];
    private readonly SemaphoreSlim startLock = new(1, 1);

    // False only while StartAsync replaces a connection of its own: the reader it ends then must not report a lost
    // connection, or the owner would start a second time from the event while this start is still under way.
    private bool raiseLostConnection = true;

    private uint requestId;
    private CancellationTokenSource? tokenSource;
    private ClientWebSocket? clientWebSocket;
    private Task? readerTask;
    private string? hashedPassword;
    private string? oldEncryptedPassword;
    private WattPilot? savedWattPilot;

    public event EventHandler<NewWattPilotFirmwareEventArgs>? NewFirmwareAvailable;
    public event EventHandler<WattPilotServiceStoppedEventArgs>? OnLostConnection;
    public event EventHandler<WattPilotUpdateEventArgs>? OnUpdate;

    private CancellationToken Token => tokenSource?.Token ?? throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);

    public WebConnection? Connection
    {
        get;

        private set => Set(ref field, value, null, () =>
        {
            if (value?.Password != null && value.EncryptedPassword != oldEncryptedPassword)
            {
                oldEncryptedPassword = value.EncryptedPassword;
                hashedPassword = null;
            }

            return value;
        });
    }

    public IReadOnlyList<WattPilotAcknowledge> UnsuccessfulWrites
    {
        get
        {
            lock (outstandingAcknowledges)
            {
                return [.. outstandingAcknowledges.Where(a => !a.IsConfirmed)];
            }
        }
    }

    [ObservableProperty]
    public partial WattPilot? WattPilot { get; set; }

    /// <summary>
    /// Connects to a charger, ending whatever connection this service already has first. Safe to call on a running
    /// service: the old reader is stopped and <see cref="OnLostConnection"/> is not raised for it, because a
    /// connection that is replaced on purpose has not been lost.
    /// </summary>
    public async ValueTask StartAsync(WebConnection connection)
    {
        // Two starts at once would each open a socket and each tear the other's fields down in their catch. The
        // second waits for the first to finish - or fail - and then replaces it like any other running connection.
        await startLock.WaitAsync().ConfigureAwait(false);

        try
        {
            await CloseAsync(false).ConfigureAwait(false);
            await ConnectAsync(connection).ConfigureAwait(false);
        }
        finally
        {
            startLock.Release();
        }
    }

    [SuppressMessage("ReSharper", "ParameterHidesMember")]
    private async ValueTask ConnectAsync(WebConnection connection)
    {
        try
        {
            tokenSource?.Dispose();
            tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            Connection = connection;

            // The PBKDF2 hash is salted with the device serial number, so it must be recomputed for
            // each connection in case we are now talking to a different WattPilot.
            hashedPassword = null;

            clientWebSocket = new ClientWebSocket();
            clientWebSocket.Options.KeepAliveInterval = TimeSpan.FromMinutes(2);
            clientWebSocket.Options.DangerousDeflateOptions = new WebSocketDeflateOptions();
            await clientWebSocket.ConnectAsync(new Uri(connection.BaseUrl + "/ws"), Token).ConfigureAwait(false);
            Token.ThrowIfCancellationRequested();

            var hello = await ReceiveTextMessage(Token).ConfigureAwait(false);
            Token.ThrowIfCancellationRequested();
            var token = ParseObject(hello);
            var type = token["type"].AsString();

            if (type == "deltaStatus" && savedWattPilot != null)
            {
                WattPilot = savedWattPilot;
                WattPilot.IsUpdating = true;
                UpdateWattPilot(WattPilot, token["status"] as JsonObject);
            }
            else
            {
                if (type != "hello")
                {
                    await Task.Delay(5000, Token).ConfigureAwait(false);
                    throw new InvalidDataException("WattPilot did not greet with 'hello'");
                }

                WattPilot = WattPilot.Parse(token);
                WattPilot.IsUpdating = true;
                savedWattPilot = null;

                var messageJson = await ReceiveTextMessage(Token).ConfigureAwait(false);
                Token.ThrowIfCancellationRequested();
                token = ParseObject(messageJson);
                type = token["type"].AsString();
                var haveData = true;

                if (type == "authRequired")
                {
                    await Authenticate(token).ConfigureAwait(false);
                    haveData = false;
                }

                while (true)
                {
                    if (!haveData)
                    {
                        messageJson = await ReceiveTextMessage(Token).ConfigureAwait(false);
                    }

                    Token.ThrowIfCancellationRequested();
                    var dataToken = ParseObject(messageJson);
                    var dataType = dataToken["type"].AsString();

                    if (dataType is "fullStatus" or "deltaStatus")
                    {
                        UpdateWattPilot(WattPilot!, dataToken["status"] as JsonObject);
                    }

                    if (dataType == "fullStatus" && dataToken["partial"].AsBoolean() != true)
                    {
                        break;
                    }

                    haveData = false;
                }
            }

            tokenSource?.Dispose();
            tokenSource = new CancellationTokenSource();
            readerTask = Task.Run(Reader, CancellationToken.None);

            //TODO: Fix new firmware detection (use ocu)
            if (WattPilot?.Version is not null && WattPilot?.LatestVersion is not null && false)
            {
                NewFirmwareAvailable?.Invoke(this, new NewWattPilotFirmwareEventArgs
                (
                    WattPilot.Version,
                    WattPilot.LatestVersion,
                    string.IsNullOrWhiteSpace(WattPilot.DeviceName) ? "WattPilot" : WattPilot.DeviceName,
                    WattPilot.SerialNumber ?? "0"
                ));
            }
        }
        catch
        {
            await CloseSocketAsync().ConfigureAwait(false);
            WattPilot?.IsUpdating = false;
            WattPilot = null;
            Connection = null;
            throw;
        }
    }

    public async ValueTask<List<string>> Send(WattPilot? localWattPilot = null, WattPilot? oldWattPilot = null)
    {
        localWattPilot ??= WattPilot ?? throw new WebException("Not connected to Wattpilot", WebExceptionStatus.ConnectionClosed);
        var sentSomething = false;
        var errors = new List<string>();

        foreach (var propertyInfo in typeof(WattPilot).GetProperties().Where(p => p.GetCustomAttributes<WattPilotAttribute>().Count() == 1))
        {
            var oldValue = oldWattPilot == null ? null : propertyInfo.GetValue(oldWattPilot);
            var newValue = propertyInfo.GetValue(localWattPilot);

            if
            (
                oldWattPilot != null &&
                (
                    ReferenceEquals(oldValue, newValue) ||
                    oldValue is not null && oldValue.Equals(newValue) ||
                    newValue is not null && newValue.Equals(oldValue) ||
                    propertyInfo.GetCustomAttribute<WattPilotAttribute>()!.IsReadOnly
                )
            )
            {
                continue;
            }

            try
            {
                sentSomething = true;
                await SendValue(localWattPilot, propertyInfo.Name).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                errors.Add($"{ex.GetType().Name}: {propertyInfo.Name} = '{propertyInfo.GetValue(localWattPilot)}' ({ex.Message})");
            }
        }

        return !sentSomething ? throw new ArgumentException(Resources.NoSettingsChanged) : errors;
    }

    public async Task RebootWattPilot()
    {
        BeginSendValues();

        if (WattPilot?.Clone() is not WattPilot newWattPilot)
        {
            throw new IOException(Resources.NoWattPilot);
        }

        newWattPilot.Reboot = true;
        var errors = await Send(newWattPilot, WattPilot).ConfigureAwait(false);

        if (errors.Count > 0)
        {
            var notWritten = "• " + string.Join(Environment.NewLine + "• ", errors);
            throw new IOException("The following settings were not written to the Wattpilot:" + Environment.NewLine + Environment.NewLine + notWritten);
        }

        await StopAsync().ConfigureAwait(false);
    }

    public void OpenConfigPdf()
    {
        var link = $"{WattPilot?.DownloadLink?.Replace("export", "documentation")}&lang={(Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName).Split('-')[0]}";
        OpenLink(link);
    }

    public void OpenChargingLog()
    {
        var link = WattPilot?.DownloadLink;
        OpenLink(link);
    }

    private void OpenLink(string? link)
    {
        if (WattPilot == null)
        {
            throw new WebSocketException(Resources.NoWattPilotConnection);
        }

        if (link == null)
        {
            throw new InvalidDataException(Resources.NoChargingLogFromWattPilot);
        }

        Process.Start(new ProcessStartInfo { FileName = link, UseShellExecute = true });
    }

    private async Task Authenticate(JsonObject token)
    {
        var token1 = token["token1"].AsString();
        var token2 = token["token2"].AsString();
        var token3 = RandomNumberGenerator.GetHexString(32, true);

        var localHashedPassword = await GetHashedPassword().ConfigureAwait(false);
        Token.ThrowIfCancellationRequested();

        var hash1Input = Encoding.UTF8.GetBytes(token1 + localHashedPassword);
        var hash1 = SHA256.HashData(hash1Input).ToHexString();
        var hashInput = Encoding.UTF8.GetBytes(token3 + token2 + hash1);
        var hash = SHA256.HashData(hashInput).ToHexString();

        Token.ThrowIfCancellationRequested();

        var authMessage = new JsonObject
        {
            { "type", "auth" },
            { "token3", token3 },
            { "hash", hash },
        }.ToJsonString();

        if (clientWebSocket == null)
        {
            throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
        }

        await clientWebSocket.SendAsync(Encoding.UTF8.GetBytes(authMessage), WebSocketMessageType.Text, WebSocketMessageFlags.DisableCompression | WebSocketMessageFlags.EndOfMessage, Token).ConfigureAwait(false);
        Token.ThrowIfCancellationRequested();
        var authResponse = ParseObject(await ReceiveTextMessage(Token).ConfigureAwait(false));

        if (authResponse["type"].AsString() == "authError")
        {
            throw new UnauthorizedAccessException(authResponse["message"].AsString());
        }

        if (authResponse["type"].AsString() != "authSuccess")
        {
            throw new InvalidDataException("The WattPilot did not respond properly on authentication");
        }
    }

    private async ValueTask<string> GetHashedPassword()
    {
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

        return hashedPassword ??= Rfc2898DeriveBytes.Pbkdf2
        (
            Encoding.UTF8.GetBytes(Connection?.Password ?? string.Empty),
            Encoding.UTF8.GetBytes(WattPilot?.SerialNumber ?? string.Empty),
            100000,
            HashAlgorithmName.SHA512,
            24
        ).ToBase64();
    }

    public ValueTask StopAsync() => CloseAsync(true);

    private async ValueTask CloseAsync(bool notifyLostConnection)
    {
        // tokenSource is the "something is running" flag: created first thing by ConnectAsync, nulled when the
        // handshake fails and when the reader ends. Nothing to cancel means nothing to wait for either.
        var source = tokenSource;

        if (source is null)
        {
            return;
        }

        raiseLostConnection = notifyLostConnection;

        try
        {
            await source.CancelAsync().ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            // The handshake or the reader tore down between the check and the cancel.
        }

        // Reader never faults (it catches everything and tears down in its finally), so awaiting it simply blocks
        // until the connection is fully closed - and until its finally has decided whether to raise the event,
        // which is why the flag is only restored afterwards.
        var task = readerTask;

        if (task != null)
        {
            await task.ConfigureAwait(false);
        }

        raiseLostConnection = true;
    }

    public void BeginSendValues()
    {
        ClearOutstandingAcknowledges();
    }

    private void ClearOutstandingAcknowledges()
    {
        lock (outstandingAcknowledges)
        {
            outstandingAcknowledges.Apply(DisposeAcknowledge);
            outstandingAcknowledges.Clear();
        }
    }

    // Must only be called while holding the lock on outstandingAcknowledges so that the Reader
    // never calls Set() on an event that is being disposed (see Reader's "response" handling).
    private static void DisposeAcknowledge(WattPilotAcknowledge acknowledge)
    {
        if (acknowledge.IsDisposed)
        {
            return;
        }

        acknowledge.IsDisposed = true;
        acknowledge.Event.Dispose();
    }

    public Task WaitSendValues(int timeout = 5000) => Task.Run(() =>
    {
        WattPilotAcknowledge[] acknowledges;

        lock (outstandingAcknowledges)
        {
            acknowledges = [.. outstandingAcknowledges];
        }

        var events = acknowledges.Select(a => a.Event.WaitHandle).ToArray();

        try
        {
            if (events.Length > 0 && !WaitHandle.WaitAll(events, timeout, true))
            {
                throw new TimeoutException(string.Format(Resources.WattPilotTimeout, timeout / 1e3d));
            }
        }
        finally
        {
            lock (outstandingAcknowledges)
            {
                acknowledges.Apply(DisposeAcknowledge);
            }
        }
    }, CancellationToken.None);

    public async ValueTask SendValue(WattPilot instance, string propertyName)
    {
        var instanceType = instance.GetType();
        var propertyInfo = instanceType.GetProperty(propertyName) ?? throw new ArgumentException(string.Format(Resources.NotAMemberOf, instanceType.Name), propertyName);
        var attribute = propertyInfo.GetCustomAttribute<WattPilotAttribute>() ?? throw new ArgumentException(string.Format(Resources.NotAMemberOf, instanceType.Name), propertyName);
        var key = attribute.TokenName ?? throw new ArgumentException(string.Format(Resources.NotAMemberOf, instanceType.Name), propertyName);

        var value = propertyInfo.GetValue(instance);

        var id = Interlocked.Increment(ref requestId);

        var data = new JsonObject
        {
            { "type", "setValue" },
            { "requestId", id },
            { "key", key },
            { "value", WattPilotExtensions.ToWattPilotJson(value, attribute) },
        }.ToJsonString();

        if (instance.IsSecured.HasValue && instance.IsSecured.Value)
        {
            var hash = HMACSHA256.HashData
            (
                Encoding.UTF8.GetBytes(await GetHashedPassword().ConfigureAwait(false)),
                Encoding.UTF8.GetBytes(data)
            ).ToHexString();

            var message = new JsonObject
            {
                { "type", "securedMsg" },
                { "data", data },
                { "requestId", FormattableString.Invariant($"{id}sm") },
                { "hmac", hash },
            };

            data = message.ToJsonString();
        }

        if (clientWebSocket == null)
        {
            throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
        }

        await clientWebSocket.SendAsync(Encoding.UTF8.GetBytes(data), WebSocketMessageType.Text, true, Token).ConfigureAwait(false);

        lock (outstandingAcknowledges)
        {
            outstandingAcknowledges.Add(new WattPilotAcknowledge(id, propertyInfo, value));
        }
    }

    /// <summary>
    /// Ends the token source and the socket. The close handshake is bounded to two seconds: a charger that has
    /// gone half open - the case the owner's watchdog reconnects for - takes the close frame and never answers
    /// it, and an unbounded CloseAsync would then hang the reader's tear-down, and with it every StopAsync and
    /// StartAsync waiting for the reader, for good. Dispose aborts whatever the handshake left.
    /// </summary>
    private async ValueTask CloseSocketAsync()
    {
        tokenSource?.Dispose();
        tokenSource = null;

        if (clientWebSocket != null)
        {
            try
            {
                using var closeTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Good bye", closeTimeout.Token).ConfigureAwait(false);
            }
            catch
            {
                // Not answered in time, or already gone either way.
            }
        }

        clientWebSocket?.Dispose();
        clientWebSocket = null;
    }

    /// <summary>The body of a WattPilot message, or an empty object where it is not one.</summary>
    private static JsonObject ParseObject(string json) => JsonNode.Parse(json)?.AsObject() ?? [];

    private void UpdateWattPilot(WattPilot instance, JsonObject? jObject)
    {
        if (jObject == null)
        {
            return;
        }

        instance.UpdateFromJson(jObject);

        if (OnUpdate != null)
        {
            _ = Task.Run(() => OnUpdate(this, new(instance, jObject)), Token);
        }
    }

    /// <summary>
    /// Receives a complete WebSocket text message, reassembling it across frames that may
    /// span multiple <see cref="WebSocket.ReceiveAsync(ArraySegment{byte},CancellationToken)"/>
    /// calls and exceed the size of <see cref="buffer"/>.
    /// </summary>
    private async ValueTask<string> ReceiveTextMessage(CancellationToken token)
    {
        if (clientWebSocket == null)
        {
            throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
        }

        var result = await clientWebSocket.ReceiveAsync(buffer, token).ConfigureAwait(false);

        if (result.MessageType == WebSocketMessageType.Close)
        {
            throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, "WattPilot closed the connection");
        }

        if (result.EndOfMessage)
        {
            return Encoding.UTF8.GetString(buffer, 0, result.Count);
        }

        using var stream = new MemoryStream(buffer.Length * 2);
        stream.Write(buffer, 0, result.Count);

        do
        {
            result = await clientWebSocket.ReceiveAsync(buffer, token).ConfigureAwait(false);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, "WattPilot closed the connection");
            }

            stream.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        return Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
    }

    private async Task Reader()
    {
        try
        {
            while (tokenSource != null && !Token.IsCancellationRequested && clientWebSocket != null)
            {
                var dataToken = ParseObject(await ReceiveTextMessage(Token).ConfigureAwait(false));
                Token.ThrowIfCancellationRequested();

                if (dataToken["type"].AsString() == "deltaStatus")
                {
                    UpdateWattPilot(WattPilot!, dataToken["status"] as JsonObject);
                }

                else if (dataToken["type"].AsString() == "response")
                {
                    JsonObject? status = null;

                    // Match the acknowledge, mark it confirmed and signal its waiter atomically while
                    // holding the lock. The IsDisposed guard (set under the same lock by WaitSendValues
                    // /ClearOutstandingAcknowledges) prevents Set() on an already-disposed event.
                    // A failed write is left unconfirmed and unsignalled so WaitSendValues times out and
                    // reports it via UnsuccessfulWrites.
                    lock (outstandingAcknowledges)
                    {
                        var ack = outstandingAcknowledges.SingleOrDefault(a => a.RequestId == dataToken["requestId"].AsUInt32());

                        if (ack != null && dataToken["success"].AsBoolean() is true)
                        {
                            ack.IsConfirmed = true;
                            status = dataToken["status"] as JsonObject;

                            if (!ack.IsDisposed)
                            {
                                ack.Event.Set();
                            }
                        }
                    }

                    if (status != null)
                    {
                        UpdateWattPilot(WattPilot!, status);
                    }
                }
            }
        }
        catch
        {
            // Just re-establish connection
        }
        finally
        {
            await CloseSocketAsync().ConfigureAwait(false);
            savedWattPilot = WattPilot;
            WattPilot?.IsUpdating = false;

            WattPilot = null;
            var connection = Connection?.Clone() as WebConnection;
            Connection = null;

            if (raiseLostConnection && OnLostConnection != null)
            {
                _ = Task.Run(() => OnLostConnection(this, new WattPilotServiceStoppedEventArgs(savedWattPilot, connection)), CancellationToken.None);
            }
        }
    }
}
