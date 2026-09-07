using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using De.Hochstaetter.HomeAutomationServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace De.Hochstaetter.HomeAutomationServer.Hubs;

/// <summary>
/// Everything that decides who may open a connection to <see cref="HomeAutomationHub"/>, in one place so that the
/// tests can check the policy the server actually runs rather than a second copy of it.
/// </summary>
public static class HubAuthentication
{
    /// <summary>
    /// Registers the ticket scheme and the service behind it. The clock is only registered if nobody has done so
    /// already, so a test can put its own in front of it.
    /// </summary>
    public static IServiceCollection AddHubTicketAuthentication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<HubTicketService>();

        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, HubTicketAuthenticationService>(HubTicketAuthenticationService.SchemeName, null);

        return services;
    }

    /// <summary>
    /// Who may open a hub connection: a client that presents a valid ticket for a user holding
    /// <see cref="Roles.User"/> or <see cref="Roles.Administrator"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The scheme has to be named explicitly. Without it the policy would fall back to the default scheme, which
    /// is Basic, and Basic credentials are exactly what must not reach the hub.
    /// </para>
    /// <para>
    /// <see cref="Roles"/> is a <see cref="FlagsAttribute"/> enum, so holding one role says nothing about the
    /// others: an administrator does not carry the User bit unless somebody set it, which is why that role is
    /// named here as well. <c>RequireRole</c> asks for **any** of the roles it is given, never all of them. The
    /// remaining roles - Guest, PowerUser, Operator, Developer - still do not get a connection on their own.
    /// </para>
    /// </remarks>
    public static AuthorizationPolicyBuilder RequireHubTicket(this AuthorizationPolicyBuilder policy) => policy
        .AddAuthenticationSchemes(HubTicketAuthenticationService.SchemeName)
        .RequireRole(nameof(Roles.User), nameof(Roles.Administrator));
}
