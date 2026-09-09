namespace De.Hochstaetter.HomeAutomationClient.Assets.Images;

/// <summary>
/// A cross, for a button that deletes or closes something. It is a <see cref="ContentControl"/> and not a
/// <see cref="Viewbox"/> like <see cref="GridIcon"/> because it needs an inherited <c>Foreground</c> to draw
/// itself with, which only a templated control has.
/// </summary>
/// <remarks>
/// Not shared with <see cref="ErrorIcon"/>, which draws a cross of its own: that one is a badge, a thick white
/// cross sized to fill a red circle, and this one is a thin line drawing that has to sit in a row of text
/// without shouting. They would have to grow a thickness and a length between them to become one control, and
/// the two are expected to keep moving apart rather than together.
/// </remarks>
public partial class CrossIcon : ContentControl
{
    public CrossIcon()
    {
        InitializeComponent();
    }
}
