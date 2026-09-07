using System.Text;
using System.Text.Encodings.Web;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace De.Hochstaetter.HomeAutomationServer.Services;

public class AuthenticationService(IOptionsMonitor<UserList> options, ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var x = options.CurrentValue.Users;
        string authHeader;

        if (Request.Headers.TryGetValue("Authorization", out var value))
        {
            authHeader = value.ToString();
        }
        else
        {
            if (!Request.Cookies.TryGetValue("auth", out authHeader!))
            {
                Logger.LogDebug("No cookie and no auth header provided");
                SetAuthHeader();
                return Task.FromResult(AuthenticateResult.NoResult());
            }
        }

        if (!authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var split = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader[6..])).Split(':', 2);

        if (split.Length != 2)
        {
            Logger.LogWarning("Incorrect auth header for basic authentication");
            SetAuthHeader();
            return Task.FromResult(AuthenticateResult.Fail("Incorrect Auth header"));
        }

        var (username, password) = (split[0], split[1]);

        var user = x.FirstOrDefault(u => u.Username == username);

        if (user == null || !user.Authenticate(password))
        {
            Logger.LogWarning("Password for user {Username} is not correct", username);
            SetAuthHeader();
            return Task.FromResult(AuthenticateResult.Fail("Access denied"));
        }

        if (Logger.IsEnabled(LogLevel.Debug))
        {
            Logger.LogDebug("User {Username} was authenticated. Roles: {Roles}", username, user.Roles);
        }

        return Task.FromResult(AuthenticateResult.Success(user.CreateAuthenticationTicket(Scheme.Name)));
    }

    private void SetAuthHeader()
    {
        Response.Headers.WWWAuthenticate = new StringValues(["Basic", "Realm=\"Home Automation Server\""]);
    }
}
