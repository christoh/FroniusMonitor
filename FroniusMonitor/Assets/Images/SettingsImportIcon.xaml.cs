namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class SettingsImportIcon
{
    public SettingsImportIcon()
    {
        InitializeComponent();
    }
}

public class SettingsImportIconExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new SettingsImportIcon();
}
