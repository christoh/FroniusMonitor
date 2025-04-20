namespace De.Hochstaetter.HomeAutomationClient.Assets.Images;

/// <summary>
///     Interaction logic for VisibilityIcon.xaml
/// </summary>
public partial class VisibilityIcon:ContentControl
{
    public static readonly StyledProperty<bool> VisibleProperty = AvaloniaProperty.Register<VisibilityIcon,bool>(nameof(Visible));

    public bool Visible
    {
        get => (bool)GetValue(VisibleProperty);
        set => SetValue(VisibleProperty, value);
    }
}

public class VisibilityIconExtension:MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new VisibilityIcon();
}
