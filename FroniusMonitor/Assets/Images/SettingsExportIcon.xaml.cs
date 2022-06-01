namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class SettingsExportIcon
{
    public SettingsExportIcon()
    {
        InitializeComponent();
    }
}

public class SettingsExportIconExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new SettingsExportIcon();
}
