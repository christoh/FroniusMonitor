using Microsoft.AspNetCore.Authorization;

namespace De.Hochstaetter.HomeAutomationServer.Models.Authorization;

/// <summary>
/// Protects an endpoint of the API with <see cref="ApiAuthenticationService"/>: a bearer token, and Basic or cookie
/// authentication where those are switched on.
/// </summary>
public class ApiAuthorizeAttribute : AuthorizeAttribute
{
    public ApiAuthorizeAttribute()
    {
        AuthenticationSchemes = ApiAuthenticationService.SchemeName;
    }
}
