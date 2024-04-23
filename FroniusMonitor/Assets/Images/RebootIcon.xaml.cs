namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class RebootIcon
{
    public RebootIcon()
    {
        InitializeComponent();
    }
}

public class RebootIconExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new RebootIcon();
}