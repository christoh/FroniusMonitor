namespace De.Hochstaetter.HomeAutomationClient.Assets.Images;

/// <summary>
/// The tree of the WPF Toshiba HVAC control, for whatever stands for outdoors. The counterpart of
/// <see cref="HouseIcon"/> and built the same way: a <see cref="ContentControl"/>, so that it has an inherited
/// <c>Foreground</c> to draw itself with.
/// </summary>
public partial class TreeIcon : ContentControl
{
    public TreeIcon()
    {
        InitializeComponent();
    }
}
