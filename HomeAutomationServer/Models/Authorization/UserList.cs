using Microsoft.AspNetCore.Authentication;

namespace De.Hochstaetter.HomeAutomationServer.Models.Authorization;

public class UserList : AuthenticationSchemeOptions
{
    public HashSet<User> Users { get; set; } = [];
}
