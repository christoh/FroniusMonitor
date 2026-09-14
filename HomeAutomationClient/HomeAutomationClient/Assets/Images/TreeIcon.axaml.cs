namespace De.Hochstaetter.HomeAutomationClient.Assets.Images;

/// <summary>
/// A tree drawn in lines, for whatever stands for outdoors. The counterpart of <see cref="HouseIcon"/> and built the
/// same way: a <see cref="ContentControl"/>, so that it has an inherited <c>Foreground</c> to draw itself with.
/// </summary>
public partial class TreeIcon : ContentControl
{
    public TreeIcon()
    {
        InitializeComponent();
    }
}
