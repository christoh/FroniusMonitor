namespace De.Hochstaetter.HomeAutomationClient.Assets.Images;

public class InfoIcon : ContentControl;

public class InfoIconExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new InfoIcon();
}
