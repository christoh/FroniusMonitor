namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class ChargingTrafficSign
{
    public ChargingTrafficSign()
    {
        InitializeComponent();
    }
}

public class ChargingTrafficSignExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new ChargingTrafficSign();
}
