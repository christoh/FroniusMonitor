using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

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
            return Task.FromResult(AuthenticateResult.Fail("Incorrect Auth header"));
        }

        var (username, password) = (split[0], split[1]);

        var user = x.FirstOrDefault(u => u.Username == username);

        if (user == null || !user.Authenticate(password))
        {
            Logger.LogWarning("Password for user {Username} is not correct", username);
            return Task.FromResult(AuthenticateResult.Fail("Access denied"));
        }

        Logger.LogDebug("User {Username} was authenticated. Roles: {Roles}", username, user.Roles);

        var identity = new Identity { IsAuthenticated = true, Name = username };
        var claims = new List<Claim>();

        claims.AddRange
        (
            Enum.GetValues<Roles>()
                .Except([Roles.All, Roles.None])
                .Where(role => (user.Roles & role) != Roles.None)
                .Select(role => new Claim(ClaimTypes.Role, role.ToString()))
        );

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(identity, claims));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipal, identity.AuthenticationType)));
    }
}
