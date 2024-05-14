namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class RefreshIcon
{
    public RefreshIcon()
    {
        InitializeComponent();
    }
}

public class RefreshIconExtension : MarkupExtension
{
    public Brush Foreground { get; set; } = Brushes.Black;
    public Brush Background { get; set; } = Brushes.Transparent;

    public override object ProvideValue(IServiceProvider serviceProvider) => new RefreshIcon { Background = Background, Foreground = Foreground };
}
