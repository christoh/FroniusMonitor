using System.Net.Http.Headers;

namespace De.Hochstaetter.HomeAutomationServer.Services.SolarWeb;

/// <summary>
///     Reads Solar.web's chart data over the same HTTP calls the chart page makes, and logs in the way the browser
///     does when Solar.web redirects to the Fronius login.
/// </summary>
/// <remarks>
///     <para>
///         Solar.web is an OpenID Connect client of the WSO2 Identity Server at <c>login.fronius.com</c>. Without a
///         session, <c>GET /Chart/GetChartNew</c> redirects through <c>/Account/ExternalLogin</c> and
///         <c>/oauth2/authorize</c> to <c>/authenticationendpoint/login.do</c>, which is the login form. The form
///         posts user name, password and its hidden fields (<c>sessionDataKey</c> above all) to <c>/commonauth</c>;
///         that redirects back to <c>/oauth2/authorize</c>, which answers a page whose only form posts <c>code</c>,
///         <c>id_token</c> and <c>state</c> to Solar.web's <c>/Account/ExternalLoginCallback</c> (<c>response_mode=form_post</c>).
///         That sets the Solar.web session cookie and redirects to the page first asked for.
///     </para>
///     <para>
///         All of that is redirects and cookies, which <see cref="HttpClientHandler" /> does by itself; what is done
///         by hand is submitting the two forms. The <c>state</c> blob and the <c>solweb_browserid_...</c> scope are
///         different on every round and are never composed here - whatever the pages hand over is sent back.
///     </para>
///     <para>
///         The login is tried once per call. If the login form comes back a second time, the credentials were
///         rejected, and that is thrown rather than tried again: Friendly Captcha is loaded on the login page and a
///         few rejected passwords bring it up, after which no program logs in until a human solves it.
///     </para>
/// </remarks>
public sealed class SolarWebClient : ISolarWebClient, IDisposable
{
    /// <summary>The login form, the code post-back, and one page in between are three; more than that is a page the flow does not know.</summary>
    private const int MaxFormHops = 4;

    private readonly ILogger<SolarWebClient> logger;
    private readonly HttpClient client;
    private readonly TimeProvider clock;

    public SolarWebClient(ILogger<SolarWebClient> logger) : this(logger, CreateHandler(), null)
    {
    }

    /// <summary>For a test that wants a handler of its own. It has to follow redirects and keep cookies, as <see cref="CreateHandler" />'s does.</summary>
    public SolarWebClient(ILogger<SolarWebClient> logger, HttpMessageHandler handler, TimeProvider? timeProvider = null)
    {
        this.logger = logger;
        clock = timeProvider ?? TimeProvider.System;
        // Solar.web has been seen to take well over 30 seconds for a day chart from the production server on
        // 2026-09-20; a timeout there costs the request and, with the request gate, delays the next one anyway.
        client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(90) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("HomeAutomationServer", GitInfo.Version.ToString()));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html", 0.9));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.1));
    }

    public static HttpClientHandler CreateHandler() => new()
    {
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 20,
        UseCookies = true,
        CookieContainer = new CookieContainer(),
        AutomaticDecompression = DecompressionMethods.All,
    };

    public async Task<SolarWebChart> GetChartAsync(SolarWebSettings settings, SolarWebInterval interval, SolarWebView view, DateOnly date, CancellationToken token = default)
    {
        var json = await GetJsonAsync(settings, ChartUri(settings, interval, view, date), token).ConfigureAwait(false);
        return SolarWebChartParser.Parse(json, settings.PvSystemId, interval, view, SolarWebPeriod.Normalize(interval, date), clock.GetUtcNow().UtcDateTime);
    }

    public async Task<SolarWebFirmwareStatus> GetFirmwareStatusAsync(SolarWebSettings settings, CancellationToken token = default)
    {
        var json = await GetJsonAsync(settings, FirmwareUri(settings), token).ConfigureAwait(false);
        return SolarWebFirmwareParser.Parse(json, settings.PvSystemId, clock.GetUtcNow().UtcDateTime);
    }

    public static Uri ChartUri(SolarWebSettings settings, SolarWebInterval interval, SolarWebView view, DateOnly date)
    {
        return new Uri(BaseUri(settings), FormattableString.Invariant(
            $"Chart/GetChartNew?pvSystemId={Uri.EscapeDataString(settings.PvSystemId)}&year={date.Year}&month={date.Month}&day={date.Day}&interval={interval.ToQueryValue()}&view={view.ToQueryValue()}"));
    }

    public static Uri FirmwareUri(SolarWebSettings settings) => new(BaseUri(settings), $"Firmware/GetComponentUpdateInfos?pvSystemId={Uri.EscapeDataString(settings.PvSystemId)}");

    private static Uri BaseUri(SolarWebSettings settings) => new(settings.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);

    private async Task<string> GetJsonAsync(SolarWebSettings settings, Uri uri, CancellationToken token)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Solar.web request: {Uri}", uri);
        }

        using var response = await FetchJsonAsync(settings, uri, token).ConfigureAwait(false);
        return await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
    }

    /// <summary>
    ///     The chart request, and the login where Solar.web answers with the login pages instead of JSON. Returns
    ///     the JSON response; the caller disposes it.
    /// </summary>
    private async Task<HttpResponseMessage> FetchJsonAsync(SolarWebSettings settings, Uri chartUri, CancellationToken token)
    {
        var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, chartUri), token).ConfigureAwait(false);
        var loginPosted = false;

        try
        {
            for (var hop = 0; hop < MaxFormHops; hop++)
            {
                if (IsJson(response))
                {
                    var result = response;
                    response = null!;
                    return result;
                }

                var pageUri = response.RequestMessage?.RequestUri ?? chartUri;
                var status = response.StatusCode;
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "no content type";
                var html = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                response.Dispose();

                var form = HtmlForm.Parse(html).FirstOrDefault(f => f.Has("sessionDataKey") || f.Has("code") || f.Has("error"));

                if (form == null)
                {
                    // Not the login and not the code: the maintenance page, an error page, a web application
                    // firewall's block page - whatever it is, it is not a wrong password, so it is not a login
                    // failure that would stop every request until a restart. The maintenance page is known and
                    // means "leave me alone for a while"; anything else is thrown as the transient failure it
                    // most likely is. Either way the log says what the page was, because the next one may be
                    // the same and somebody has to be able to see it.
                    var summary = HtmlForm.Summarize(html, 400);
                    var description = $"Solar.web answered {pageUri.GetLeftPart(UriPartial.Path)} with {(int)status} {contentType} instead of JSON: {summary}";

                    if (logger.IsEnabled(LogLevel.Warning))
                    {
                        logger.LogWarning("{Description}", description);
                    }

                    throw IsMaintenancePage(summary) ? new SolarWebUnavailableException(null, description) : new InvalidDataException(description);
                }

                if (form.Has("sessionDataKey"))
                {
                    if (loginPosted)
                    {
                        throw new SolarWebLoginException($"login.fronius.com rejected the user name or the password of {settings.UserName}");
                    }

                    if (settings.Password.Length == 0)
                    {
                        throw new SolarWebLoginException($"No password is configured for the Solar.web account {settings.UserName}");
                    }

                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation("No Solar.web session, logging in as {UserName}", settings.UserName);
                    }

                    loginPosted = true;

                    // The visible field is usernameUserInput; the page's script copies it into the hidden username
                    // before submitting, and that hidden one is what /commonauth reads. "Remember me" asks for the
                    // longer lived single sign-on cookie, so that the next expired Solar.web session is renewed
                    // without a password.
                    response = await SendAsync(new HttpRequestMessage(HttpMethod.Post, Resolve(pageUri, form.Action))
                    {
                        Content = form.ToContent(
                            new KeyValuePair<string, string>("username", settings.UserName),
                            new KeyValuePair<string, string>("usernameUserInput", settings.UserName),
                            new KeyValuePair<string, string>("password", settings.Password),
                            new KeyValuePair<string, string>("chkRemember", "on")),
                    }, token).ConfigureAwait(false);

                    continue;
                }

                if (form.Has("error"))
                {
                    throw new SolarWebLoginException($"login.fronius.com answered the error '{form["error"]}': {form["error_description"]}");
                }

                // The authorization code goes back to Solar.web, which sets its session cookie and redirects to
                // the page first asked for - normally the chart itself. Where it lands somewhere else, the chart is
                // asked for once more, now with the session.
                response = await SendAsync(new HttpRequestMessage(HttpMethod.Post, Resolve(pageUri, form.Action)) { Content = form.ToContent() }, token).ConfigureAwait(false);

                if (!IsJson(response))
                {
                    response.Dispose();
                    response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, chartUri), token).ConfigureAwait(false);
                }
            }

            throw new SolarWebLoginException($"The Solar.web login did not finish within {MaxFormHops} pages");
        }
        catch
        {
            response?.Dispose();
            throw;
        }
    }

    /// <summary>Sends, and turns a 429 and a 503 into their own exceptions before any other status is judged.</summary>
    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
        {
            var status = response.StatusCode;
            var retryAfter = RetryAfter(response);
            var message = $"Solar.web answered {(int)status} {status} to {request.RequestUri?.GetLeftPart(UriPartial.Path)}";
            response.Dispose();
            throw status == HttpStatusCode.TooManyRequests ? new SolarWebRateLimitException(retryAfter, message) : new SolarWebUnavailableException(retryAfter, message);
        }

        if (!response.IsSuccessStatusCode)
        {
            var status = response.StatusCode;
            var uri = response.RequestMessage?.RequestUri ?? request.RequestUri;
            response.Dispose();
            throw new HttpRequestException($"Solar.web answered {(int)status} {status} to {uri?.GetLeftPart(UriPartial.Path)}", null, status);
        }

        return response;
    }

    private TimeSpan? RetryAfter(HttpResponseMessage response) => response.Headers.RetryAfter is { } header
        ? header.Delta ?? (header.Date is { } date ? date - clock.GetUtcNow() : null)
        : null;

    /// <summary>
    ///     Fronius' maintenance page, seen on Sunday 2026-09-20: a plain page headed "Maintenance Work" and
    ///     "Wartungsarbeiten", "This page is therefore temporarily unavailable. We will be back online soon."
    /// </summary>
    public static bool IsMaintenancePage(string text) => text.Contains("Maintenance Work", StringComparison.OrdinalIgnoreCase) || text.Contains("Wartungsarbeiten", StringComparison.OrdinalIgnoreCase);

    private static bool IsJson(HttpResponseMessage response) => response.Content.Headers.ContentType?.MediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true;

    private static Uri Resolve(Uri page, string? action) => string.IsNullOrEmpty(action) ? page : new Uri(page, action);

    public void Dispose() => client.Dispose();
}
