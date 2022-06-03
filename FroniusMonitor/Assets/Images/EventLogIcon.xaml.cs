namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class EventLogIcon
{
    public EventLogIcon()
    {
        InitializeComponent();
    }
}

public class EventLogIconExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new EventLogIcon();
}
