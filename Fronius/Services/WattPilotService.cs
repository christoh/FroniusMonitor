﻿using De.Hochstaetter.Fronius.Models.Events;

namespace De.Hochstaetter.Fronius.Services;

public class WattPilotService(SettingsBase settings) : BindableBase, IWattPilotService
{
    private uint requestId;
    private CancellationTokenSource? tokenSource;
    private ClientWebSocket? clientWebSocket;
    private Thread? readThread;
    private readonly List<WattPilotAcknowledge> outstandingAcknowledges = [];
    private readonly byte[] buffer = new byte[8192];
    private string? hashedPassword;
    private string? oldEncryptedPassword;
    private WattPilot? savedWattPilot;

    public event EventHandler<NewWattPilotFirmwareEventArgs>? NewFirmwareAvailable;

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
            WattPilotAcknowledge[] result;

            lock (outstandingAcknowledges)
            {
                result = [.. outstandingAcknowledges];
            }

            return result;
        }
    }

    public WattPilot? WattPilot
    {
        get;
        private set => Set(ref field, value);
    }

    [SuppressMessage("ReSharper", "ParameterHidesMember")]
    public async ValueTask Start(WebConnection connection)
    {
        try
        {
            tokenSource?.Dispose();
            tokenSource = new CancellationTokenSource(10000);
            Connection = connection;

            clientWebSocket = new ClientWebSocket();
            clientWebSocket.Options.KeepAliveInterval = TimeSpan.FromMinutes(2);
            clientWebSocket.Options.DangerousDeflateOptions = new WebSocketDeflateOptions();
            await clientWebSocket.ConnectAsync(new Uri(connection.BaseUrl + "/ws"), Token).ConfigureAwait(false);
            Token.ThrowIfCancellationRequested();

            var result = await clientWebSocket.ReceiveAsync(buffer, Token).ConfigureAwait(false);
            Token.ThrowIfCancellationRequested();
            var hello = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var token = JObject.Parse(hello);
            var type = token["type"]?.Value<string>();

            if (type == "deltaStatus" && savedWattPilot != null)
            {
                WattPilot = savedWattPilot;
                WattPilot.IsUpdating = true;
                UpdateWattPilot(WattPilot, token["status"] as JObject);
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

                result = await clientWebSocket.ReceiveAsync(buffer, Token).ConfigureAwait(false);
                Token.ThrowIfCancellationRequested();
                var auth = Encoding.UTF8.GetString(buffer, 0, result.Count);
                token = JObject.Parse(auth);
                type = token["type"]?.Value<string>();
                var haveData = true;

                if (type == "authRequired")
                {
                    await Authenticate(token).ConfigureAwait(false);
                    haveData = false;
                }

                while (true)
                {
                    if (!haveData) result = await clientWebSocket.ReceiveAsync(buffer, Token).ConfigureAwait(false);
                    Token.ThrowIfCancellationRequested();
                    var dataToken = JObject.Parse(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    var dataType = dataToken["type"]?.Value<string>();

                    if (dataType is "fullStatus" or "deltaStatus")
                    {
                        UpdateWattPilot(WattPilot!, dataToken["status"] as JObject);
                    }

                    if (dataType == "fullStatus" && !dataToken["partial"]!.Value<bool>())
                    {
                        break;
                    }

                    haveData = false;
                }
            }

            tokenSource?.Dispose();
            tokenSource = new CancellationTokenSource();
            readThread = new Thread(Reader);
            readThread.Start();

            if (WattPilot?.Version is not null && WattPilot?.LatestVersion is not null && WattPilot.LatestVersion > WattPilot.Version)
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
            tokenSource?.Dispose();
            tokenSource = null;

            if (clientWebSocket != null)
            {
                try
                {
                    await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Good bye", CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                    //
                }
            }

            clientWebSocket?.Dispose();
            clientWebSocket = null;

            if (WattPilot != null)
            {
                WattPilot.IsUpdating = false;
            }

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

        if (!sentSomething)
        {
            throw new ArgumentException(Resources.NoSettingsChanged);
        }

        return errors;
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

        await Stop().ConfigureAwait(false);
    }

    public void OpenConfigPdf()
    {
        var link = $"{WattPilot?.DownloadLink?.Replace("export", "documentation")}&lang={(settings.Language ?? CultureInfo.CurrentUICulture.Name).Split('-')[0]}";
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

    private async Task Authenticate(JObject token)
    {
        var token1 = token["token1"]?.Value<string>();
        var token2 = token["token2"]?.Value<string>();
        var token3Bytes = RandomNumberGenerator.GetBytes(16);

        var token3 = string.Join(string.Empty, token3Bytes.Select(b => b.ToString("x2")));

        var localHashedPassword = await GetHashedPassword().ConfigureAwait(false);
        Token.ThrowIfCancellationRequested();

        var hash = await Task.Run(() =>
        {
            var hash1Input = Encoding.UTF8.GetBytes(token1 + localHashedPassword);
            var hash1 = string.Join(string.Empty, SHA256.HashData(hash1Input).Select(b => b.ToString("x2")));
            var hashInput = Encoding.UTF8.GetBytes(token3 + token2 + hash1);
            return string.Join(string.Empty, SHA256.HashData(hashInput).Select(b => b.ToString("x2")));
        }, Token).ConfigureAwait(false);

        Token.ThrowIfCancellationRequested();

        var authMessage = new JObject
        {
            { "type", "auth" },
            { "token3", token3 },
            { "hash", hash },
        }.ToString();

        if (clientWebSocket == null) throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);

        await clientWebSocket.SendAsync(Encoding.UTF8.GetBytes(authMessage), WebSocketMessageType.Text, WebSocketMessageFlags.DisableCompression | WebSocketMessageFlags.EndOfMessage, Token).ConfigureAwait(false);
        Token.ThrowIfCancellationRequested();
        var result = await clientWebSocket.ReceiveAsync(buffer, Token).ConfigureAwait(false);

        Token.ThrowIfCancellationRequested();
        var authResponse = JObject.Parse(Encoding.UTF8.GetString(buffer, 0, result.Count));

        if (authResponse["type"]?.Value<string>() == "authError")
        {
            throw new UnauthorizedAccessException(authResponse["message"]?.Value<string>());
        }

        if (authResponse["type"]?.Value<string>() != "authSuccess")
        {
            throw new InvalidDataException("The WattPilot did not respond properly on authentication");
        }
    }

    private async ValueTask<string> GetHashedPassword()
    {
        if (hashedPassword == null)
        {
            await Task.Run(() =>
            {
                using var deriveBytes = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(Connection?.Password ?? string.Empty), Encoding.UTF8.GetBytes(WattPilot?.SerialNumber ?? string.Empty), 100000, HashAlgorithmName.SHA512);
                var hash0 = deriveBytes.GetBytes(24);
                hashedPassword = Convert.ToBase64String(hash0);
            }, Token).ConfigureAwait(false);
        }

        return hashedPassword!;
    }

    public async ValueTask Stop()
    {
        if (tokenSource != null)
        {
            await tokenSource.CancelAsync().ConfigureAwait(false);
        }

        while (Connection != null)
        {
            await Task.Delay(50, CancellationToken.None).ConfigureAwait(false);
        }
    }

    public void BeginSendValues()
    {
        ClearOutstandingAcknowledges();
    }

    private void ClearOutstandingAcknowledges()
    {
        lock (outstandingAcknowledges)
        {
            outstandingAcknowledges.Apply(a => a.Event.Dispose());
            outstandingAcknowledges.Clear();
        }
    }

    public Task WaitSendValues(int timeout = 5000) => Task.Run(() =>
    {
        WaitHandle[] events;

        lock (outstandingAcknowledges)
        {
            events = outstandingAcknowledges.Select(a => a.Event.WaitHandle).ToArray();
        }

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
                outstandingAcknowledges.Apply(a => a.Event.Dispose());
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

        // ReSharper disable once ReplaceConditionalExpressionWithNullCoalescing
        var data = new JObject
        {
            { "type", "setValue" },
            { "requestId", id },
            { "key", key },
            {
                "value",
                value == null ? null :
                value is bool boolValue ? boolValue :
                value is byte byteValue ? byteValue :
                value is uint uintValue ? uintValue :
                value is long longValue ? longValue :
                value is Enum enumValue ? (int)Convert.ChangeType(enumValue, TypeCode.Int32) :
                value is int intValue ? intValue :
                value is string stringValue ? stringValue :
                value is double doubleValue ? doubleValue :
                value is float floatValue ? floatValue :
                value is IEnumerable<byte> bytes ? JArray.FromObject(bytes.Select(b => (int)b)) :
                value is IEnumerable<int> integers ? JArray.FromObject(integers) :
                attribute.Type != null && attribute.Type.IsInstanceOfType(value) ? JToken.FromObject(value) :
                throw new NotSupportedException("Unsupported Type")
            },
        }.ToString();

        await Task.Run(() =>
        {
            if (instance.IsSecured.HasValue && instance.IsSecured.Value)
            {
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(GetHashedPassword().Result));
                var hash = string.Join(string.Empty, hmac.ComputeHash(Encoding.UTF8.GetBytes(data)).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));

                var message = new JObject
                {
                    { "type", "securedMsg" },
                    { "data", data },
                    { "requestId", FormattableString.Invariant($"{id}sm") },
                    { "hmac", hash },
                };

                data = message.ToString();
            }
        }, CancellationToken.None).ConfigureAwait(false);

        if (clientWebSocket == null) throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
        await clientWebSocket.SendAsync(Encoding.UTF8.GetBytes(data), WebSocketMessageType.Text, true, Token).ConfigureAwait(false);

        lock (outstandingAcknowledges)
        {
            outstandingAcknowledges.Add(new WattPilotAcknowledge(id, propertyInfo, value));
        }
    }

    private void UpdateWattPilot(object instance, JObject? jObject)
    {
        if (jObject == null)
        {
            return;
        }

        foreach (var token in jObject)
        {
            //Debug.Print($"{token.Key}: {token.Value?.ToString().Replace("\r", "").Replace("\n", "")}");
            var propertyInfos = instance.GetType().GetProperties().Where(p => p.GetCustomAttributes<WattPilotAttribute>().Any(a => a.TokenName == token.Key)).ToArray();

            switch (propertyInfos.Length)
            {
                case 0:
                    continue;

                case 1:
                    SetWattPilotValue(instance, propertyInfos[0], token.Value);
                    continue;
            }

            if (token.Value is JArray array)
            {
                foreach (var propertyInfo in propertyInfos)
                {
                    var attribute = propertyInfo.GetCustomAttributes<WattPilotAttribute>().SingleOrDefault(a => a.TokenName == token.Key);

                    if (attribute?.Index >= 0 && attribute.Index < array.Count)
                    {
                        SetWattPilotValue(instance, propertyInfo, array[attribute.Index]);
                    }
                }
            }
        }
    }

    private void SetWattPilotValue(object instance, PropertyInfo propertyInfo, JToken? token)
    {
        try
        {
            propertyInfo.SetValue(instance, token?.ToObject(propertyInfo.PropertyType));
            return;
        }
        catch
        {
            //
        }

        if (token is JObject subObject)
        {
            var subInstance = Activator.CreateInstance(propertyInfo.PropertyType);

            if (subInstance != null)
            {
                propertyInfo.SetValue(instance, subInstance);
                UpdateWattPilot(subInstance, subObject);
                return;
            }
        }

        var stringValue = token?.Value<string>();

        if (propertyInfo.PropertyType.IsAssignableFrom(typeof(IPAddress)))
        {
            propertyInfo.SetValue(instance, stringValue == null ? null : IPAddress.Parse(stringValue));
            return;
        }

        Debugger.Break(); // Unhandled Json
    }

    private async void Reader()
    {
        try
        {
            while (tokenSource != null && !Token.IsCancellationRequested && clientWebSocket != null)
            {
                var result = await clientWebSocket.ReceiveAsync(buffer, Token);
                Token.ThrowIfCancellationRequested();
                var dataToken = JObject.Parse(Encoding.UTF8.GetString(buffer, 0, result.Count));
                Token.ThrowIfCancellationRequested();

                if (dataToken["type"]?.Value<string>() == "deltaStatus")
                {
                    UpdateWattPilot(WattPilot!, dataToken["status"] as JObject);
                }

                else if (dataToken["type"]?.Value<string>() == "response")
                {
                    WattPilotAcknowledge? ack;

                    lock (outstandingAcknowledges)
                    {
                        ack = outstandingAcknowledges.SingleOrDefault(a => a.RequestId == dataToken["requestId"]?.Value<uint>());
                    }

                    if (ack == null) continue;

                    try
                    {
                        if (dataToken["success"]?.Value<bool>() is not true)
                        {
                            ack.Event.Dispose();
                        }
                        else
                        {
                            ack.Event.Set();
                            UpdateWattPilot(WattPilot!, dataToken["status"] as JObject);
                        }
                    }
                    finally
                    {
                        lock (outstandingAcknowledges)
                        {
                            outstandingAcknowledges.Remove(ack);
                        }
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
            tokenSource?.Dispose();
            tokenSource = null;

            if (clientWebSocket != null)
            {
                try
                {
                    await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Good bye", CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                    //
                }
            }

            clientWebSocket?.Dispose();
            clientWebSocket = null;
            savedWattPilot = WattPilot;

            if (WattPilot != null)
            {
                WattPilot.IsUpdating = false;
            }

            WattPilot = null;
            readThread = null;
            Connection = null;
        }
    }
}
