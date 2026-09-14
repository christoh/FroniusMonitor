namespace De.Hochstaetter.Fronius.Models.WebApi;

/// <summary>
/// The roles a user of the home automation server can hold. Flags without a hierarchy: holding one says nothing
/// about the others. Shared between the server, which grants them, and the client, which shows them.
/// <see cref="Guest"/> on its own sees the inverters with their smart meters and batteries, and nothing that is
/// switched, charged, configured or paid for - see <see cref="RolesExtensions.SeesAllDevices"/>.
/// </summary>
[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Roles
{
    None = 0,
    Guest = 1 << 0,
    User = 1 << 1,
    PowerUser = 1 << 2,
    Operator = 1 << 3,
    Administrator = 1 << 4,
    Developer = 1 << 5,
    All = Guest | User | PowerUser | Operator | Administrator | Developer,
}