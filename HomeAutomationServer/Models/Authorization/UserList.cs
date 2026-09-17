using Microsoft.AspNetCore.Authentication;

namespace De.Hochstaetter.HomeAutomationServer.Models.Authorization;

public class UserList : AuthenticationSchemeOptions
{
    public HashSet<User> Users { get; set; } = [];

    /// <summary>
    /// <see cref="Settings.EnableGuestAccount"/>, as the authentication code sees it.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="Users"/>, which is the very <see cref="HashSet{T}"/> the settings own, this is a copy
    /// taken once while <c>Program.cs</c> configures the options. Switching the account on or off therefore
    /// takes a restart of the server, which is what editing <c>Settings.xml</c> means anyway.
    /// </remarks>
    public bool EnableGuestAccount { get; set; } = true;
}
