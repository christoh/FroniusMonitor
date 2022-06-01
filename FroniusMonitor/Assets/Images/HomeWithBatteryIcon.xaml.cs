namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class HomeWithBatteryIcon
{
    public HomeWithBatteryIcon()
    {
        InitializeComponent();
    }
}

public class HomeWithBatteryIconExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new HomeWithBatteryIcon();
}
