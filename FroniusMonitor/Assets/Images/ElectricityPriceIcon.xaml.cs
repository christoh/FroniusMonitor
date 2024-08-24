namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class ElectricityPriceIcon
{
    public ElectricityPriceIcon()
    {
        InitializeComponent();
    }
}

public class ElectricityPriceIconExtension : MarkupExtension
{
    public Brush Foreground { get; set; } = Brushes.Black;
    public Brush Background { get; set; } = Brushes.Transparent;

    public override object ProvideValue(IServiceProvider serviceProvider) => new ElectricityPriceIcon { Foreground = Foreground, Background = Background };
}
