namespace De.Hochstaetter.HomeAutomationClient.Assets.Images;

/// <summary>
/// A person in a circle, for the place in the menu bar that says who is logged in. A <see cref="ContentControl"/>
/// like <see cref="CrossIcon"/>, so that it has an inherited <c>Foreground</c> to draw itself with.
/// </summary>
public partial class UserIcon : ContentControl
{
    public UserIcon()
    {
        InitializeComponent();
    }
}