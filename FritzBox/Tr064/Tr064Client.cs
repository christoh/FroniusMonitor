namespace De.Hochstaetter.FritzBox.Tr064;

public sealed class Tr064ClientOptions
{
    /// <summary>The box's TR-064 root, for example <c>http://192.168.44.11:49000</c>.</summary>
    public required Uri BaseUri { get; init; }

    public string? UserName { get; init; }

    public string? Password { get; init; }

    /// <summary>
    /// Sends the actions over HTTPS, to the port the box names with <c>DeviceInfo:GetSecurityPort</c>, once the
    /// description has been read over plain HTTP. Some actions carry passwords, so this is the default.
    /// </summary>
    public bool UseTls { get; init; } = true;

    /// <summary>
    /// Decides on a certificate the operating system does not trust, which the box's own self-signed one never is.
    /// <see langword="null"/> rejects it.
    /// </summary>
    public Func<X509Certificate2, bool>? AcceptUntrustedCertificate { get; init; }

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>How often the state of a second factor process is asked for while the box waits for the user.</summary>
    public TimeSpan SecondFactorPollInterval { get; init; } = TimeSpan.FromSeconds(1);
}

/// <summary>
/// A client for the TR-064 (SOAP) interface of a FRITZ!Box. It needs "Allow access for applications" in the box's
/// network settings and, for most actions, a user with the matching rights.
/// </summary>
public sealed class Tr064Client : IDisposable
{
    private const string AuthService = "X_AVM-DE_Auth";

    private readonly Tr064ClientOptions options;
    private readonly ILogger logger;
    private readonly HttpClient httpClient;
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<Tr064Argument>>> actionsByService = new();
    private Uri controlBaseUri;

    public Tr064Client(Tr064ClientOptions options, ILogger<Tr064Client>? logger = null) : this(options, logger, CreateHandler(options)) { }

    internal Tr064Client(Tr064ClientOptions options, ILogger<Tr064Client>? logger, HttpMessageHandler handler)
    {
        this.options = options;
        this.logger = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        httpClient = new HttpClient(handler) { Timeout = options.Timeout };
        controlBaseUri = options.BaseUri;
    }

    public Tr064Description? Description { get; private set; }

    /// <summary>
    /// Reads the description and, with <see cref="Tr064ClientOptions.UseTls"/>, switches to HTTPS. Must be called
    /// before anything else.
    /// </summary>
    public async Task<Tr064Description> ConnectAsync(CancellationToken token = default)
    {
        var xml = await httpClient.GetStringAsync(new Uri(options.BaseUri, "tr64desc.xml"), token).ConfigureAwait(false);
        Description = Tr064Description.Parse(xml);
        controlBaseUri = options.BaseUri;

        if (options.UseTls)
        {
            var result = await InvokeAsync("DeviceInfo", "GetSecurityPort", [], token).ConfigureAwait(false);
            controlBaseUri = new UriBuilder(options.BaseUri) { Scheme = Uri.UriSchemeHttps, Port = int.Parse(result["NewSecurityPort"], CultureInfo.InvariantCulture) }.Uri;
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation
            (
                "TR-064: {Model} with FRITZ!OS {Version}, {ServiceCount} services, actions go to {ControlUri}",
                Description.ModelName, Description.SoftwareVersion, Description.Services.Count, controlBaseUri
            );
        }

        return Description;
    }

    /// <summary>
    /// Calls <paramref name="action"/> of the service named <paramref name="serviceName"/> (for example
    /// <c>DeviceInfo</c>) and returns its output arguments by name.
    /// </summary>
    /// <exception cref="Tr064Exception">The box answered with a UPnP error.</exception>
    /// <exception cref="UnauthorizedAccessException">The box refused the credentials.</exception>
    public async Task<IReadOnlyDictionary<string, string>> InvokeAsync
    (
        string serviceName,
        string action,
        IEnumerable<KeyValuePair<string, string>> arguments,
        CancellationToken token = default,
        string? secondFactorToken = null
    )
    {
        var service = GetService(serviceName);

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(controlBaseUri, service.ControlUrl))
        {
            Content = new StringContent(Tr064Soap.CreateRequest(service.ServiceType, action, arguments, secondFactorToken), Encoding.UTF8, "text/xml"),
        };

        request.Headers.TryAddWithoutValidation("SOAPACTION", $"\"{service.ServiceType}#{action}\"");

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("TR-064: calling {Service}:{Action}", serviceName, action);
        }

        using var response = await httpClient.SendAsync(request, token).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException($"The box refused the credentials for {serviceName}:{action}");
        }

        var xml = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

        // A SOAP fault comes with status 500 and is turned into a Tr064Exception by ParseResponse.
        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.InternalServerError)
        {
            response.EnsureSuccessStatusCode();
        }

        return Tr064Soap.ParseResponse(xml, action);
    }

    /// <summary>
    /// Like <see cref="InvokeAsync"/>, but if the box wants a second factor for the action, starts it, tells
    /// <paramref name="onConfirmationRequired"/> how to confirm (for example <c>button</c> or
    /// <c>dtmf;*11234</c>), waits until somebody did and calls the action again. A box with the confirmation switched
    /// off never asks.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, string>> InvokeWithSecondFactorAsync
    (
        string serviceName,
        string action,
        IReadOnlyCollection<KeyValuePair<string, string>> arguments,
        Func<string, CancellationToken, Task> onConfirmationRequired,
        TimeSpan timeout,
        CancellationToken token = default
    )
    {
        try
        {
            return await InvokeAsync(serviceName, action, arguments, token).ConfigureAwait(false);
        }
        catch (Tr064Exception ex) when (ex.ErrorCode == Tr064Exception.SecondFactorRequired)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("TR-064: {Service}:{Action} needs a second factor", serviceName, action);
            }
        }

        var start = await InvokeAsync(AuthService, "SetConfig", [new("NewAction", "start")], token).ConfigureAwait(false);
        var authToken = start["NewToken"];
        var state = start.GetValueOrDefault("NewState");
        await onConfirmationRequired(start.GetValueOrDefault("NewMethods") ?? string.Empty, token).ConfigureAwait(false);
        var started = DateTime.UtcNow;

        while (state != "authenticated")
        {
            if (state is "blocked" or "failure" or "stopped" or "anotherauthprocess" or "disabled")
            {
                throw new UnauthorizedAccessException($"The second factor for {serviceName}:{action} ended in state '{state}'");
            }

            if (DateTime.UtcNow - started > timeout)
            {
                await TryStopSecondFactorAsync(authToken).ConfigureAwait(false);
                throw new TimeoutException($"Nobody confirmed {serviceName}:{action} on the box within {timeout}");
            }

            await Task.Delay(options.SecondFactorPollInterval, token).ConfigureAwait(false);
            state = (await InvokeAsync(AuthService, "GetState", [], token, authToken).ConfigureAwait(false)).GetValueOrDefault("NewState");
        }

        return await InvokeAsync(serviceName, action, arguments, token, authToken).ConfigureAwait(false);
    }

    /// <summary>
    /// The actions of a service with their arguments, read from its SCPD on the box. That is the box's own word on
    /// what an action takes, which can differ from the documentation between FRITZ!OS versions.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, IReadOnlyList<Tr064Argument>>> GetActionsAsync(string serviceName, CancellationToken token = default)
    {
        if (actionsByService.TryGetValue(serviceName, out var cached))
        {
            return cached;
        }

        var service = GetService(serviceName);
        var xml = await httpClient.GetStringAsync(new Uri(options.BaseUri, service.ScpdUrl), token).ConfigureAwait(false);
        var root = XDocument.Parse(xml).Root ?? throw new InvalidDataException($"The SCPD of {serviceName} is empty");
        var ns = root.Name.Namespace;

        var actions = root.Descendants(ns + "action").ToDictionary
        (
            a => a.Element(ns + "name")?.Value ?? string.Empty,
            IReadOnlyList<Tr064Argument> (a) => a.Descendants(ns + "argument")
                .Select(arg => new Tr064Argument(arg.Element(ns + "name")?.Value ?? string.Empty, arg.Element(ns + "direction")?.Value == "out"))
                .ToList()
        );

        actionsByService[serviceName] = actions;
        return actions;
    }

    public void Dispose() => httpClient.Dispose();

    private Tr064Service GetService(string serviceName) =>
        (Description ?? throw new InvalidOperationException($"Call {nameof(ConnectAsync)} first")).FindService(serviceName) ??
        throw new NotSupportedException($"The box does not offer the TR-064 service {serviceName}");

    private async Task TryStopSecondFactorAsync(string authToken)
    {
        try
        {
            await InvokeAsync(AuthService, "SetConfig", [new("NewAction", "stop")], CancellationToken.None, authToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex, "TR-064: could not stop the second factor process");
            }
        }
    }

    private static SocketsHttpHandler CreateHandler(Tr064ClientOptions options) => new()
    {
        // SocketsHttpHandler answers the box's digest challenge itself.
        Credentials = options.UserName == null && options.Password == null ? null : new NetworkCredential(options.UserName ?? string.Empty, options.Password ?? string.Empty),
        SslOptions = new SslClientAuthenticationOptions
        {
            RemoteCertificateValidationCallback = (_, certificate, _, errors) =>
                errors == SslPolicyErrors.None ||
                certificate != null && options.AcceptUntrustedCertificate?.Invoke(certificate as X509Certificate2 ?? X509CertificateLoader.LoadCertificate(certificate.GetRawCertData())) == true,
        },
    };
}

/// <summary>An argument of a TR-064 action as the service's SCPD declares it.</summary>
public sealed record Tr064Argument(string Name, bool IsOutput);
