using System.Security.Principal;

namespace De.Hochstaetter.HomeAutomationServer.Models.Authorization;

public class Identity : IIdentity
{
    public string AuthenticationType => "Basic";
    public bool IsAuthenticated { get; set; }
    public string? Name { get; set; }
}
