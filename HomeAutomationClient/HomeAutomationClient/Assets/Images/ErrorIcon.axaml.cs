namespace De.Hochstaetter.HomeAutomationClient.Assets.Images;

public partial class ErrorIcon : ContentControl
{
    public ErrorIcon()
    {
        InitializeComponent();
    }
}

public class ErrorIconExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new ErrorIcon();
}
