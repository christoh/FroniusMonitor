using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace De.Hochstaetter.HomeAutomationServer.Services;

/// <summary>
/// The registration of <see cref="ApiAuthenticationService"/>, in one place so that the tests host the scheme the
/// server actually runs rather than a second copy of it. The counterpart of <c>HubAuthentication</c>.
/// </summary>
public static class ApiAuthentication
{
    /// <summary>
    /// Registers the API scheme and the token store behind it. The clock is only registered if nobody has done so
    /// already, so a test can put its own in front of it.
    /// </summary>
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<BearerTokenService>();

        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, ApiAuthenticationService>(ApiAuthenticationService.SchemeName, null);

        return services;
    }
}
