using System.Text.Encodings.Web;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Primitives;

namespace De.Hochstaetter.HomeAutomationServer.Services;

/// <summary>
/// Authenticates a call to the API. A bearer token from <see cref="BearerTokenService"/> is always accepted; Basic
/// credentials and the <see cref="AuthCookie"/> only where <see cref="AuthenticationSettings"/>
/// switches them on, because both are there for debugging.
/// </summary>
public sealed class ApiAuthenticationService(BearerTokenService tokens, IOptionsMonitor<UserList> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>The scheme <see cref="ApiAuthorizeAttribute"/> asks for.</summary>
    public const string SchemeName = "Api";

    /// <summary>
    /// The cookie that carries the bearer token where <see cref="AuthenticationSettings.EnableCookieAuthentication"/>
    /// is on. It holds the whole header value, <c>Bearer</c> and all, so that it is read exactly like the header.
    /// </summary>
    public const string AuthCookie = "auth";

    private const string Realm = "Realm=\"Home Automation Server\"";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var settings = options.CurrentValue.Authentication;
        var authHeader = Request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(authHeader) && settings.EnableCookieAuthentication)
        {
            authHeader = Request.Cookies[AuthCookie];
        }

        if (string.IsNullOrEmpty(authHeader))
        {
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug("No credentials were provided");
            }

            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (GetCredentials(authHeader, "Bearer") is { } token)
        {
            if (tokens.Validate(token) is not { } tokenUser)
            {
                // BearerTokenService has already logged why.
                return Task.FromResult(AuthenticateResult.Fail("The bearer token is not valid"));
            }

            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug("User {Username} was authenticated by bearer token. Roles: {Roles}", tokenUser.Username, tokenUser.Roles);
            }

            return Task.FromResult(AuthenticateResult.Success(tokenUser.CreateAuthenticationTicket(Scheme.Name)));
        }

        if (GetCredentials(authHeader, "Basic") is not { } basic)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!settings.EnableBasicAuthentication)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning("Basic credentials were refused from {Ip}: EnableBasicAuthentication is off in {FileName}", Context.Connection.RemoteIpAddress, Settings.SettingsFileName);
            }

            return Task.FromResult(AuthenticateResult.Fail("Basic authentication is switched off"));
        }

        string[] split;

        try
        {
            split = Encoding.UTF8.GetString(Convert.FromBase64String(basic)).Split(':', 2);
        }
        catch (FormatException)
        {
            split = [];
        }

        if (split.Length != 2)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning("Incorrect auth header for basic authentication");
            }

            return Task.FromResult(AuthenticateResult.Fail("Incorrect Auth header"));
        }

        var (username, password) = (split[0], split[1]);

        var user = options.CurrentValue.Find(username);

        if (user == null || !user.Authenticate(password))
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning("Password for user {Username} is not correct", username);
            }

            return Task.FromResult(AuthenticateResult.Fail("Access denied"));
        }

        if (Logger.IsEnabled(LogLevel.Debug))
        {
            Logger.LogDebug("User {Username} was authenticated by Basic credentials. Roles: {Roles}", username, user.Roles);
        }

        return Task.FromResult(AuthenticateResult.Success(user.CreateAuthenticationTicket(Scheme.Name)));
    }

    /// <summary>
    /// Says which schemes a refused request could have used. Basic is only offered where it would be accepted: a
    /// browser answers a Basic challenge with a login box of its own, and that must not pop up in front of the
    /// browser client for nothing.
    /// </summary>
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = options.CurrentValue.Authentication.EnableBasicAuthentication
            ? new StringValues([$"Bearer {Realm}", $"Basic {Realm}"])
            : new StringValues($"Bearer {Realm}");

        return base.HandleChallengeAsync(properties);
    }

    /// <summary>
    /// What follows <paramref name="scheme"/> in an <c>Authorization</c> header value, or <see langword="null"/>
    /// where the value is of another scheme.
    /// </summary>
    public static string? GetCredentials(string? authorization, string scheme) =>
        authorization is not null && authorization.Length > scheme.Length && authorization[scheme.Length] == ' ' && authorization.StartsWith(scheme, StringComparison.OrdinalIgnoreCase)
            ? authorization[(scheme.Length + 1)..].Trim()
            : null;
}
