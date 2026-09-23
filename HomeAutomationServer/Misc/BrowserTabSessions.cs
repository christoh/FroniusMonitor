using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace De.Hochstaetter.HomeAutomationServer.Misc;

/// <summary>
/// Browser tabs the client opens with a <see cref="BrowserTabTicket"/>, such as the OpenAPI document: the ticket in
/// the address is swapped for a cookie holding a bearer token, and the tab is sent to the same address without it.
/// </summary>
/// <remarks>
/// <para>
/// The cookie is limited to <see cref="PathPrefix"/> twice over: the browser only sends it there, and
/// <see cref="ApiAuthenticationService"/> only accepts it there. It is <c>HttpOnly</c>, because no script has any
/// business reading it, <c>SameSite=Strict</c>, because the CORS policy lets every origin send requests with
/// credentials, and always <c>Secure</c>, because the https is done by an ingress in front of the server. It lives as long as a bearer token and is not renewed; a tab that has outlived it is opened again
/// from the app.
/// </para>
/// <para>
/// This is not the debugging cookie <see cref="AuthenticationSettings.EnableCookieAuthentication"/> switches on,
/// and it does not depend on it: it reaches nothing but <see cref="PathPrefix"/>, and it is only ever set in
/// exchange for a ticket that a logged-in client asked for.
/// </para>
/// </remarks>
public static class BrowserTabSessions
{
    /// <summary>What a browser tab session reaches, and all it reaches: the OpenAPI document, which <c>MapOpenApi</c> serves below it.</summary>
    public const string PathPrefix = "/openapi";

    /// <summary>The cookie that holds the bearer token of a browser tab.</summary>
    public const string Cookie = "tab";

    /// <summary>Whether a request is one the cookie of a browser tab may authenticate.</summary>
    public static bool Covers(PathString path) => path.StartsWithSegments(PathPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Swaps a ticket in the address for the cookie. Has to come before the authentication, so that the ticket never
    /// reaches the endpoint: the request that carries it is answered with a redirect and nothing else.
    /// </summary>
    public static IApplicationBuilder UseBrowserTabSessions(this IApplicationBuilder app) => app.Use(async (context, next) =>
    {
        var request = context.Request;

        if (!Covers(request.Path) || !request.Query.TryGetValue(BrowserTabTicket.QueryParameter, out var ticket))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var tokens = context.RequestServices.GetRequiredService<BearerTokenService>();

        // A ticket that does not hold up gets no cookie but the same redirect: the tab then shows the 401 of the
        // endpoint, which says what is wrong better than anything that could be written here.
        if (tokens.RedeemTabTicket(ticket.ToString()) is { } user)
        {
            var token = tokens.Issue(user);

            context.Response.Cookies.Append(Cookie, token.Value, new CookieOptions
            {
                Path = PathPrefix,
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                // Always, although Kestrel itself only ever sees http: in practice it runs in a container behind an
                // ingress that does the https, and request.IsHttps only says so where that ingress is listed in
                // WebServerSettings/TrustedProxies (see ReverseProxies) - a setting to forget, not to rely on. Without
                // it the browser would also send the token over plain http, e.g. on the request the ingress then
                // redirects to https. Where the server is reached over plain http directly, the browser drops the
                // cookie, and the tab gets a 401 - only http://localhost is exempt, as a secure context.
                Secure = true,
                MaxAge = token.Lifetime,
            });

            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(BrowserTabSessions));

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("{Username} opened a browser tab for {Path} from {Ip}", user.Username, request.Path.Value, context.Connection.RemoteIpAddress);
            }
        }

        // The same address without the ticket, so that it stays neither in the address bar nor in the history.
        var query = QueryString.Create(request.Query.Where(pair => pair.Key != BrowserTabTicket.QueryParameter));
        context.Response.Redirect(request.PathBase + request.Path + query);
    });
}
