namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class PdfIcon
{
    public PdfIcon()
    {
        InitializeComponent();
    }
}

public class PdfIconExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new PdfIcon();
}
