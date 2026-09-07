using System.Security.Principal;

namespace De.Hochstaetter.HomeAutomationServer.Models.Authorization;

public class Identity : IIdentity
{
    /// <summary>
    /// The scheme that authenticated the request. Basic is only the default because it was the first scheme;
    /// <see cref="AuthorizationExtensions.CreateAuthenticationTicket"/> always sets it explicitly.
    /// </summary>
    public string AuthenticationType { get; init; } = "Basic";
    public bool IsAuthenticated { get; set; }
    public string? Name { get; set; }
}
