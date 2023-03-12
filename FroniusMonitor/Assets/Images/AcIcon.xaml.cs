namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class AcIcon
{
    public AcIcon()
    {
        InitializeComponent();
    }
}

public class AcIconExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new AcIcon();
}
