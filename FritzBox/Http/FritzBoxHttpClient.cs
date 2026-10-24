using System.Xml;

namespace De.Hochstaetter.FritzBox.Http;

public sealed class FritzBoxHttpClientOptions
{
    /// <summary>The box's web interface, for example <c>http://192.168.44.11</c>.</summary>
    public required Uri BaseUri { get; init; }

    /// <summary>The box user. If empty, the user the box names as the last one to log in is taken.</summary>
    public string? UserName { get; init; }

    public required string Password { get; init; }

    /// <summary>
    /// Decides on a certificate the operating system does not trust, which the box's own is when it is addressed by IP.
    /// <see langword="null"/> rejects it.
    /// </summary>
    public Func<X509Certificate2, bool>? AcceptUntrustedCertificate { get; init; }

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>The longest login block (after wrong passwords) that is waited out rather than reported.</summary>
    public TimeSpan MaximumBlockTime { get; init; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// The web interface of a FRITZ!Box with a session ID: the login, the AHA interface and the Smart Home REST API. The
/// session is created when it is first needed and again when the box no longer accepts it.
/// </summary>
public sealed class FritzBoxHttpClient : IDisposable
{
    private const string NoSession = "0000000000000000";
    private const string LoginPath = "login_sid.lua?version=2";

    private readonly FritzBoxHttpClientOptions options;
    private readonly ILogger logger;
    private readonly HttpClient httpClient;
    private readonly SemaphoreSlim loginLock = new(1, 1);
    private string? sessionId;

    public FritzBoxHttpClient(FritzBoxHttpClientOptions options, ILogger<FritzBoxHttpClient>? logger = null) : this(options, logger, CreateHandler(options)) { }

    internal FritzBoxHttpClient(FritzBoxHttpClientOptions options, ILogger<FritzBoxHttpClient>? logger, HttpMessageHandler handler)
    {
        this.options = options;
        this.logger = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        httpClient = new HttpClient(handler) { BaseAddress = options.BaseUri, Timeout = options.Timeout };
    }

    public Uri BaseUri => options.BaseUri;

    /// <summary>
    /// The FRITZ!OS version from <c>jason_boxinfo.xml</c>, which needs no login: <c>&lt;j:Version&gt;254.08.25&lt;/j:Version&gt;</c>
    /// is hardware 254 with FRITZ!OS 8.25. <see langword="null"/> when the box does not have the file or it says
    /// nothing readable.
    /// </summary>
    public async Task<Version?> GetFritzOsVersionAsync(CancellationToken token = default)
    {
        using var response = await httpClient.GetAsync("jason_boxinfo.xml", token).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        XDocument document;

        try
        {
            document = XDocument.Parse(await response.Content.ReadAsStringAsync(token).ConfigureAwait(false));
        }
        catch (XmlException)
        {
            return null;
        }

        var parts = (document.Root?.Elements().FirstOrDefault(e => e.Name.LocalName == "Version")?.Value ?? string.Empty).Split('.');

        return parts.Length >= 3 &&
               int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var major) &&
               int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var minor)
            ? new Version(major, minor)
            : null;
    }

    /// <summary>Logs in now, also when there is a session already. Every other call logs in by itself when it has to.</summary>
    public async Task LogInAsync(CancellationToken token = default)
    {
        await loginLock.WaitAsync(token).ConfigureAwait(false);

        try
        {
            sessionId = await LoginAsync(token).ConfigureAwait(false);
        }
        finally
        {
            loginLock.Release();
        }
    }

    /// <summary>Returns the AHA answer to <c>webservices/homeautoswitch.lua?&lt;query&gt;</c>.</summary>
    public async Task<string> GetAhaAsync(string query, CancellationToken token = default)
    {
        using var response = await SendAsync(session => new HttpRequestMessage(HttpMethod.Get, $"webservices/homeautoswitch.lua?{query}&sid={session}"), token).ConfigureAwait(false);
        return await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
    }

    /// <summary>Returns the JSON of the REST API at <c>api/v0/&lt;path&gt;</c>.</summary>
    public async Task<string> GetRestAsync(string path, CancellationToken token = default)
    {
        using var response = await SendAsync(session => CreateRestRequest(HttpMethod.Get, path, session, null), token).ConfigureAwait(false);
        return await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
    }

    /// <summary>Sends <paramref name="json"/> to the REST API at <c>api/v0/&lt;path&gt;</c>.</summary>
    public async Task SendRestAsync(HttpMethod method, string path, string json, CancellationToken token = default)
    {
        using var response = await SendAsync(session => CreateRestRequest(method, path, session, json), token).ConfigureAwait(false);
    }

    public void Dispose()
    {
        httpClient.Dispose();
        loginLock.Dispose();
    }

    private static HttpRequestMessage CreateRestRequest(HttpMethod method, string path, string session, string? json)
    {
        var request = new HttpRequestMessage(method, "api/v0/" + path.TrimStart('/'));
        request.Headers.TryAddWithoutValidation("Authorization", "AVM-SID " + session);

        if (json != null)
        {
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return request;
    }

    /// <summary>
    /// Sends the request <paramref name="createRequest"/> makes for the current session. When the box refuses the
    /// session, it logs in again and repeats the request once.
    /// </summary>
    /// <exception cref="HttpRequestException">The box answered with an error. The message holds its answer.</exception>
    private async Task<HttpResponseMessage> SendAsync(Func<string, HttpRequestMessage> createRequest, CancellationToken token)
    {
        for (var attempt = 1; ; attempt++)
        {
            var session = await GetSessionIdAsync(token).ConfigureAwait(false);
            using var request = createRequest(session);
            var response = await httpClient.SendAsync(request, token).ConfigureAwait(false);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden && attempt == 1)
            {
                response.Dispose();
                Interlocked.CompareExchange(ref sessionId, null, session);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                response.Dispose();
                throw new HttpRequestException($"{request.Method} {request.RequestUri} failed with {(int)response.StatusCode} {response.StatusCode}: {body}", null, response.StatusCode);
            }

            return response;
        }
    }

    private async Task<string> GetSessionIdAsync(CancellationToken token)
    {
        if (sessionId is { } current)
        {
            return current;
        }

        await loginLock.WaitAsync(token).ConfigureAwait(false);

        try
        {
            return sessionId ??= await LoginAsync(token).ConfigureAwait(false);
        }
        finally
        {
            loginLock.Release();
        }
    }

    private async Task<string> LoginAsync(CancellationToken token)
    {
        var state = XDocument.Parse(await httpClient.GetStringAsync(LoginPath, token).ConfigureAwait(false)).Root ??
                    throw new InvalidDataException("login_sid.lua answered with an empty document");

        var blockTime = TimeSpan.FromSeconds(int.TryParse(state.Element("BlockTime")?.Value, CultureInfo.InvariantCulture, out var seconds) ? seconds : 0);

        if (blockTime > options.MaximumBlockTime)
        {
            throw new UnauthorizedAccessException($"The box blocks logins for {blockTime.TotalSeconds:N0} s, probably after wrong passwords");
        }

        if (blockTime > TimeSpan.Zero)
        {
            await Task.Delay(blockTime, token).ConfigureAwait(false);
        }

        var challenge = state.Element("Challenge")?.Value ?? throw new InvalidDataException("login_sid.lua sent no challenge");

        var userName = string.IsNullOrEmpty(options.UserName)
            ? state.Element("Users")?.Elements("User").FirstOrDefault(u => u.Attribute("last")?.Value == "1")?.Value ?? string.Empty
            : options.UserName;

        using var content = new FormUrlEncodedContent([new("username", userName), new("response", FritzBoxLogin.CreateResponse(challenge, options.Password))]);
        using var response = await httpClient.PostAsync(LoginPath, content, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = XDocument.Parse(await response.Content.ReadAsStringAsync(token).ConfigureAwait(false)).Root;
        var session = result?.Element("SID")?.Value;

        if (string.IsNullOrEmpty(session) || session == NoSession)
        {
            throw new UnauthorizedAccessException($"The box refused the login of '{userName}'");
        }

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("FRITZ!Box: logged in as {UserName} with {Method}", userName, challenge.StartsWith("2$", StringComparison.Ordinal) ? "PBKDF2" : "MD5");
        }

        return session;
    }

    private static SocketsHttpHandler CreateHandler(FritzBoxHttpClientOptions options) => new()
    {
        SslOptions = new SslClientAuthenticationOptions
        {
            RemoteCertificateValidationCallback = (_, certificate, _, errors) =>
                errors == SslPolicyErrors.None ||
                certificate != null && options.AcceptUntrustedCertificate?.Invoke(certificate as X509Certificate2 ?? X509CertificateLoader.LoadCertificate(certificate.GetRawCertData())) == true,
        },
    };
}
