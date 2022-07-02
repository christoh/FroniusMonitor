namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class Car
{
    public Car()
    {
        InitializeComponent();
    }
}

public class CarExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new Car();
}
