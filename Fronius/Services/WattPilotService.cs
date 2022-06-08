using System.Diagnostics;
using System.Net.WebSockets;

namespace De.Hochstaetter.Fronius.Services;

public class WattPilotService : BindableBase, IWattPilotService
{
    private CancellationTokenSource? tokenSource;
    private CancellationToken Token => tokenSource!.Token;
    private ClientWebSocket? clientWebSocket;
    private static readonly Random random = new Random(unchecked((int)DateTime.UtcNow.Ticks));
    private Thread? readThread;

    private WebConnection? connection;

    public WebConnection? Connection
    {
        get => connection;
        private set => Set(ref connection, value);
    }

    private WattPilot? wattPilot;

    public WattPilot? WattPilot
    {
        get => wattPilot;
        private set => Set(ref wattPilot, value);
    }

    [SuppressMessage("ReSharper", "ParameterHidesMember")]
    public async Task Start(WebConnection connection)
    {
        try
        {
            tokenSource?.Dispose();
            tokenSource = new CancellationTokenSource(10000);
            Connection = connection;
            clientWebSocket = new ClientWebSocket();
            await clientWebSocket.ConnectAsync(new Uri(connection.BaseUrl + "/ws"), Token).ConfigureAwait(false);
            Token.ThrowIfCancellationRequested();
            var buffer = new byte[8192];

            var result = await clientWebSocket.ReceiveAsync(buffer, Token).ConfigureAwait(false);
            Token.ThrowIfCancellationRequested();
            var hello = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var token = JObject.Parse(hello);
            var type = token["type"]?.Value<string>();

            if (type != "hello")
            {
                throw new InvalidDataException("WattPilot did not greet with 'hello'");
            }

            WattPilot = WattPilot.Parse(token);

            result = await clientWebSocket.ReceiveAsync(buffer, Token).ConfigureAwait(false);
            Token.ThrowIfCancellationRequested();
            var auth = Encoding.UTF8.GetString(buffer, 0, result.Count);
            token = JObject.Parse(auth);
            var token1 = token["token1"]?.Value<string>();
            var token2 = token["token2"]?.Value<string>();
            type = token["type"]?.Value<string>();

            if (type != "authRequired" || token1?.Length != 32 || token2?.Length != 32)
            {
                throw new InvalidDataException("WattPilot did not supply proper auth tokens");
            }

            var token3Bytes = new byte[16];
            random.NextBytes(token3Bytes);
            var token3 = string.Join(string.Empty, token3Bytes.Select(b => b.ToString("x2")));

            using var deriveBytes = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(Connection.Password), Encoding.UTF8.GetBytes(WattPilot.SerialNumber ?? string.Empty), 100000, HashAlgorithmName.SHA512);
            var hash0 = deriveBytes.GetBytes(256);
            var hashedPassword = Convert.ToBase64String(hash0)[..32];
            using var sha256 = SHA256.Create();
            var hash1Input = Encoding.UTF8.GetBytes(token1 + hashedPassword);
            var hash1 = string.Join(string.Empty, sha256.ComputeHash(hash1Input, 0, hash1Input.Length).Select(b => b.ToString("x2")));
            var hashInput = Encoding.UTF8.GetBytes(token3 + token2 + hash1);
            var hash = string.Join(string.Empty, sha256.ComputeHash(hashInput, 0, hashInput.Length).Select(b => b.ToString("x2")));

            var authMessage = new JObject
            {
                {"type", "auth"},
                {"token3", token3},
                {"hash", hash},
            }.ToString();

            await clientWebSocket.SendAsync(Encoding.UTF8.GetBytes(authMessage), WebSocketMessageType.Text, true, Token).ConfigureAwait(false);
            Token.ThrowIfCancellationRequested();
            result = await clientWebSocket.ReceiveAsync(buffer, Token).ConfigureAwait(false);
            Token.ThrowIfCancellationRequested();
            var authResponse = Encoding.UTF8.GetString(buffer, 0, result.Count);


            JObject? dataToken;
            string? dataType;

            do
            {
                result = await clientWebSocket.ReceiveAsync(buffer, Token).ConfigureAwait(false);
                Token.ThrowIfCancellationRequested();
                dataToken = JObject.Parse(Encoding.UTF8.GetString(buffer, 0, result.Count));
                dataType = dataToken["type"]?.Value<string>();

                if (dataType == "fullStatus")
                {
                    UpdateWattPilot(dataToken["status"] as JObject);
                }
            } while (dataType == "fullStatus" && dataToken["partial"]?.Value<bool>() is true);

            tokenSource?.Dispose();
            tokenSource = new CancellationTokenSource();
            readThread = new Thread(Reader);
            readThread.Start();
        }
        catch
        {
            tokenSource?.Dispose();
            tokenSource = null;
            clientWebSocket?.Dispose();
            clientWebSocket = null;
            WattPilot = null;
            Connection = null;
            throw;
        }
    }

    public async Task Stop()
    {
        tokenSource?.Cancel();

        while (Connection != null)
        {
            await Task.Delay(200, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private void UpdateWattPilot(JObject? jObject)
    {
        if (jObject == null || WattPilot == null)
        {
            return;
        }

        foreach (var token in jObject)
        {
            IReadOnlyList<PropertyInfo> propertyInfos = typeof(WattPilot).GetProperties().Where(p => p.GetCustomAttribute<WattPilotAttribute>() is { } attribute && attribute.TokenName == token.Key).ToArray();

            if (!propertyInfos.Any())
            {
                continue;
            }

            if (propertyInfos.Count == 1)
            {
                SetWattPilotValue(propertyInfos[0], token.Value);
                return;
            }

            if (token.Value is JArray array)
            {
                foreach (var propertyInfo in propertyInfos)
                {
                    var attribute = propertyInfo.GetCustomAttribute<WattPilotAttribute>();

                    if (attribute?.Index >= 0 && attribute.Index < array.Count)
                    {
                        SetWattPilotValue(propertyInfo,array[attribute.Index]);
                    }
                }
            }
        }
    }

    private void SetWattPilotValue(PropertyInfo propertyInfo, JToken? token)
    {
        var nonNullableType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

        if
        (
            SetValue<bool>() ||
            SetValue<double>() ||
            SetValue<int>() ||
            SetValue<byte>() ||
            SetValue<string>() ||
            SetValue<DateTime>()
        )
        {
            return;
        }

        var stringValue = token?.Value<string>();

        if (stringValue == null)
        {
            propertyInfo.SetValue(WattPilot, null);
            return;
        }

        if (nonNullableType.IsEnum)
        {
            propertyInfo.SetValue(WattPilot, Enum.Parse(nonNullableType, stringValue));
            return;
        }

        if (propertyInfo.PropertyType.IsAssignableFrom(typeof(Version)))
        {
            propertyInfo.SetValue(WattPilot, new Version(stringValue));
            return;
        }

        Debugger.Break();

        bool SetValue<T>()
        {
            if (!propertyInfo.PropertyType.IsAssignableFrom(typeof(T)))
            {
                return false;
            }

            if (token != null)
            {
                propertyInfo.SetValue(WattPilot, token.Value<T>());
                return true;
            }

            propertyInfo.SetValue(WattPilot, null);
            return true;
        }
    }

    private async void Reader()
    {
        var buffer = new byte[8192];

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
                    UpdateWattPilot(dataToken["status"] as JObject);
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
            clientWebSocket?.Dispose();
            clientWebSocket = null;
            WattPilot = null;
            readThread = null;
            Connection = null;
        }
    }
}
