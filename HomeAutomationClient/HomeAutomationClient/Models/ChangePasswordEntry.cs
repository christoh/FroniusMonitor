namespace De.Hochstaetter.HomeAutomationClient.Models;

/// <summary>
/// The entry of the Settings menu that lets a user change their own password. Its own type, the same way as
/// <see cref="UserManagementEntry"/>, so <c>MainViewModel.Settings</c> can tell it apart from a device.
/// </summary>
public sealed class ChangePasswordEntry
{
    public static ChangePasswordEntry Instance { get; } = new();

    private ChangePasswordEntry() { }

    public override string ToString() => Loc.ChangePassword;
}
