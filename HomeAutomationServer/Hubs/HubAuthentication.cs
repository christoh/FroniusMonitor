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
    /// <see cref="Roles.User"/>.
    /// </summary>
    /// <remarks>
    /// The scheme has to be named explicitly. Without it the policy would fall back to the default scheme, which
    /// is Basic, and Basic credentials are exactly what must not reach the hub.
    /// </remarks>
    public static AuthorizationPolicyBuilder RequireHubTicket(this AuthorizationPolicyBuilder policy) => policy
        .AddAuthenticationSchemes(HubTicketAuthenticationService.SchemeName)
        .RequireRole(nameof(Roles.User));
}
