namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class ChargingTrafficSignSettings
{
    public ChargingTrafficSignSettings()
    {
        InitializeComponent();
    }
}

public class ChargingTrafficSignSettingsExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new ChargingTrafficSignSettings();
}
