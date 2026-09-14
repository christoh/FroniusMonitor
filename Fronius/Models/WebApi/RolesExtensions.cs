namespace De.Hochstaetter.Fronius.Models.WebApi;

/// <summary>
/// What a set of <see cref="Roles"/> lets a client see. Shared between the server, which filters what it hands
/// out, and the client, which hides what it would not get anyway - so that the two never disagree.
/// </summary>
public static class RolesExtensions
{
    /// <summary>
    /// Whether these roles see every device and the price data: <see cref="Roles.User"/> or
    /// <see cref="Roles.Administrator"/>, the two that got onto the hub before there were guests. Anyone else the
    /// hub admits is a guest and sees the inverters with their smart meters and batteries, and nothing that is
    /// switched, charged, configured or paid for. Flags: a guest who also holds User is a user.
    /// </summary>
    public static bool SeesAllDevices(this Roles roles) => (roles & (Roles.User | Roles.Administrator)) != Roles.None;
}
