namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

/// <summary>One check box of the role list in the user editor.</summary>
public sealed partial class RoleItem(Roles role) : ObservableObject
{
    /// <summary>The role names are the server's enum names on purpose, not translated - see <c>MainViewModel.User</c>.</summary>
    public string Name { get; } = role.ToString();

    public Roles Role { get; } = role;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}