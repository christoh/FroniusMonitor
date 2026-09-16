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

    /// <summary>
    /// The user of that name, or <see langword="null"/> where there is none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every resolution of a user name goes through here, because <see cref="User.Guest"/> is in no user list and
    /// searching the list alone therefore answers that it does not exist. That was the bug this replaced: the
    /// controller knew about the guest and <c>HubTicketService.Validate</c> did not, so a guest was handed a hub
    /// ticket that the hub then refused, and the guest saw nothing at all.
    /// </para>
    /// <para>
    /// <c>FirstOrDefault</c>, not <c>SingleOrDefault</c>: two users of the same name is a hand edited user list,
    /// and taking the whole API down at authentication time - including the endpoints needed to repair it - is
    /// the worse of the two ways to react to that.
    /// </para>
    /// </remarks>
    public static User? Find(this UserList users, string? userName) => userName switch
    {
        null => null,
        _ when users.IsBuiltInGuest(userName) => User.Guest,
        _ => users.Users.FirstOrDefault(u => string.Equals(userName, u.Username, StringComparison.OrdinalIgnoreCase)),
    };

    /// <summary>
    /// Whether that name belongs to <see cref="User.Guest"/> rather than to anybody in the list. While the account
    /// is switched on the name is reserved: no user of it can be added, changed or deleted. While it is off there
    /// is nothing special about it at all, and a user may be called <c>guest</c> like any other.
    /// </summary>
    public static bool IsBuiltInGuest(this UserList users, string? userName) => users.EnableGuestAccount && User.IsGuest(userName);

    /// <summary>The roles back out of the claims <see cref="CreateAuthenticationTicket"/> put in, as the flags they were.</summary>
    public static Roles GetRoles(this ClaimsPrincipal principal) => principal.FindAll(ClaimTypes.Role)
        .Select(claim => Enum.TryParse<Roles>(claim.Value, out var role) ? role : Roles.None)
        .Aggregate(Roles.None, (all, role) => all | role);

    /// <inheritdoc cref="RolesExtensions.SeesAllDevices"/>
    public static bool SeesAllDevices(this ClaimsPrincipal principal) => principal.GetRoles().SeesAllDevices();
}
