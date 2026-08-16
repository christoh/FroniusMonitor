namespace De.Hochstaetter.HomeAutomationClient.Assets.Images;

public partial class ErrorIcon : ContentControl;

public class ErrorIconExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new ErrorIcon();
}
