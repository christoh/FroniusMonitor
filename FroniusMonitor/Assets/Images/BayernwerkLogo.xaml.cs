namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class BayernwerkLogo
{
    public BayernwerkLogo()
    {
        InitializeComponent();
    }
}

public class BayernwerkLogoExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new BayernwerkLogo();
}
