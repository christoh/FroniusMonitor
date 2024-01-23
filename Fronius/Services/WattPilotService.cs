namespace De.Hochstaetter.Fronius.Services;

public class WattPilotService : BindableBase, IWattPilotService
{
    private uint requestId;
    private CancellationTokenSource? tokenSource;
    private ClientWebSocket? clientWebSocket;
    private static readonly Random random = new(unchecked((int)DateTime.UtcNow.Ticks));
    private Thread? readThread;
    private readonly List<WattPilotAcknowledge> outstandingAcknowledges = new();
    private readonly byte[] buffer = new byte[8192];
    private string? hashedPassword;
    private string? oldEncryptedPassword;
    private WattPilot? savedWattPilot;

    private CancellationToken Token => tokenSource?.Token ?? throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);

    private WebConnection? connection;

    public WebConnection? Connection
    {
        get => connection;

        private set => Set(ref connection, value, null, () =>
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
                result = outstandingAcknowledges.ToArray();
            }

            return result;
        }
    }

    private WattPilot? wattPilot;

    public WattPilot? WattPilot
    {
        get => wattPilot;
        private set => Set(ref wattPilot, value);
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

    private async Task Authenticate(JObject token)
    {
        var token1 = token["token1"]?.Value<string>();
        var token2 = token["token2"]?.Value<string>();
        var token3Bytes = new byte[16];

        random.NextBytes(token3Bytes);
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

    private async Task<string> GetHashedPassword()
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
            await Task.Delay(200, CancellationToken.None).ConfigureAwait(false);
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

        var data = new JObject
        {
            { "type", "setValue" },
            { "requestId", id },
            { "key", key },
            {
                "value",
                value switch
                {
                    null => null,
                    bool boolValue => boolValue,
                    byte byteValue => byteValue,
                    uint uintValue => uintValue,
                    Enum enumValue => (int)Convert.ChangeType(enumValue, TypeCode.Int32),
                    int intValue => intValue,
                    string stringValue => stringValue,
                    double doubleValue => doubleValue,
                    float floatValue => floatValue,
                    _ => throw new NotSupportedException("Unsupported Type")
                }
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
            outstandingAcknowledges.Add(new WattPilotAcknowledge { RequestId = id, PropertyInfo = propertyInfo, Value = value });
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
            Debug.Print($"{token.Key}: {token.Value}");
            IReadOnlyList<PropertyInfo> propertyInfos = instance.GetType().GetProperties().Where(p => p.GetCustomAttributes<WattPilotAttribute>().Any(a => a.TokenName == token.Key)).ToArray();

            if (!propertyInfos.Any())
            {
                continue;
            }

            if (propertyInfos.Count == 1)
            {
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
        var nonNullableType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

        if
        (
            SetValue<bool>() ||
            SetValue<float>() ||
            SetValue<double>() ||
            SetValue<int>() ||
            SetValue<long>() ||
            SetValue<byte>() ||
            SetValue<string>()
        )
        {
            return;
        }

        if (propertyInfo.Name == nameof(WattPilot.Cards))
        {
            if (token is JArray jArray)
            {
                var cards = new WattPilotCard[jArray.Count];

                for (var i = 0; i < jArray.Count; i++)
                {
                    if (jArray[i] is not JObject jObject) continue;
                    cards[i] = new WattPilotCard();
                    UpdateWattPilot(cards[i], jObject);
                }

                propertyInfo.SetValue(instance, cards);
                return;
            }

            propertyInfo.SetValue(instance, null);
            return;
        }

        {
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
        }

        {
            if (token is JArray array)
            {
                if (Activator.CreateInstance(propertyInfo.PropertyType) is not IList list)
                {
                    return;
                }

                var subType = propertyInfo.PropertyType.GetGenericArguments()[0];

                foreach (var arrayItem in array)
                {
                    if (arrayItem is not JObject subObject || IoC.Get(subType) is not { } subInstance)
                    {
                        continue;
                    }

                    UpdateWattPilot(subInstance, subObject);
                    list.Add(subInstance);
                }

                propertyInfo.SetValue(instance, list);
                return;
            }
        }

        var stringValue = token?.Value<string>();

        if (stringValue == null)
        {
            propertyInfo.SetValue(instance, null);
            return;
        }

        if (propertyInfo.PropertyType.IsAssignableFrom(typeof(IPAddress)))
        {
            propertyInfo.SetValue(instance, IPAddress.Parse(stringValue));
            return;
        }

        if (propertyInfo.PropertyType.IsAssignableFrom(typeof(DateTime)))
        {
            if (DateTime.TryParse(stringValue + "Z", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTime))
            {
                propertyInfo.SetValue(instance, dateTime);
            }

            if (stringValue.LastIndexOf('.') >= 0)
            {
                var dateString = stringValue[..stringValue.LastIndexOf('.')] + 'Z';

                if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out dateTime))
                {
                    propertyInfo.SetValue(instance, dateTime);
                }
            }

            return;
        }

        if (nonNullableType.IsEnum)
        {
            propertyInfo.SetValue(instance, Enum.Parse(nonNullableType, stringValue));
            return;
        }

        if (propertyInfo.PropertyType.IsAssignableFrom(typeof(Version)))
        {
            propertyInfo.SetValue(instance, new Version(stringValue));
            return;
        }

        Debugger.Break();

        bool SetValue<T>()
        {
            if (!propertyInfo.PropertyType.IsAssignableFrom(typeof(T)))
            {
                return false;
            }

            if (token?.Value<string>() != null)
            {
                propertyInfo.SetValue(instance, token.Value<T>());
                return true;
            }

            propertyInfo.SetValue(instance, null);
            return true;
        }
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
            WattPilot = null;
            readThread = null;
            Connection = null;
        }
    }
}
