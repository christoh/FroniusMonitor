namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class EnergyFLow
{
    public EnergyFLow()
    {
        InitializeComponent();
    }
}

public class EnergyFLowExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new EnergyFLow();
}
