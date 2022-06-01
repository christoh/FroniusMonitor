namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class SettingsIcon
{
    public SettingsIcon()
    {
        InitializeComponent();
    }
}

public class SettingsIconExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new SettingsIcon();
}
