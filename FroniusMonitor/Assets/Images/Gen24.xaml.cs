namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class Gen24
{
    public Gen24()
    {
        InitializeComponent();
    }
}

public class Gen24Extension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new Gen24();
}
