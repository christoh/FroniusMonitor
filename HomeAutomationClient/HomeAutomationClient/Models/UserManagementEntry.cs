namespace De.Hochstaetter.HomeAutomationClient.Models;

/// <summary>
/// The entry of the Settings menu that is not a device. The menu shows each entry through its
/// <see cref="ToString"/>, and <c>MainViewModel.Settings</c> tells the entries apart by type.
/// </summary>
public sealed class UserManagementEntry
{
    public static UserManagementEntry Instance { get; } = new();

    private UserManagementEntry()
    {
    }

    public override string ToString() => Loc.UserManagement;
}