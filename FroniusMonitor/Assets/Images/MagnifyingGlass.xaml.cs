namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class MagnifyingGlass
{
    public MagnifyingGlass()
    {
        InitializeComponent();
    }
}

public class MagnifyingGlassExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new MagnifyingGlass();
}