using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace De.Hochstaetter.HomeAutomationServer.Models.Authorization;

public static class AuthorizationExtensions
{
    /// <summary>
    /// The ticket every authentication scheme of this server hands back: the name of the user plus one role claim
    /// for each role they hold. <see cref="Roles.All"/> and <see cref="Roles.None"/> are not roles of their own and
    /// never become a claim.
    /// </summary>
    /// <param name="scheme">
    /// The name of the scheme that authenticated the request. It has to be the registered name, because an
    /// authorization policy that names its schemes matches against exactly that.
    /// </param>
    public static AuthenticationTicket CreateAuthenticationTicket(this User user, string scheme)
    {
        var identity = new Identity { AuthenticationType = scheme, IsAuthenticated = true, Name = user.Username };

        var claims = Enum.GetValues<Roles>()
            .Except([Roles.All, Roles.None])
            .Where(role => (user.Roles & role) != Roles.None)
            .Select(role => new Claim(ClaimTypes.Role, role.ToString()));

        return new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(identity, claims)), scheme);
    }

    /// <summary>The roles back out of the claims <see cref="CreateAuthenticationTicket"/> put in, as the flags they were.</summary>
    public static Roles GetRoles(this ClaimsPrincipal principal) => principal.FindAll(ClaimTypes.Role)
        .Select(claim => Enum.TryParse<Roles>(claim.Value, out var role) ? role : Roles.None)
        .Aggregate(Roles.None, (all, role) => all | role);

    /// <inheritdoc cref="RolesExtensions.SeesAllDevices"/>
    public static bool SeesAllDevices(this ClaimsPrincipal principal) => principal.GetRoles().SeesAllDevices();
}
