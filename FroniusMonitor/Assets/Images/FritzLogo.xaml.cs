namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class FritzLogo
{
    public FritzLogo()
    {
        InitializeComponent();
    }
}

public class FritzLogoExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new FritzLogo();
}
