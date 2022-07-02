namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class Chevron
{
    public static readonly DependencyProperty AngleProperty = DependencyProperty.Register
    (
        nameof(Angle), typeof(double), typeof(Chevron)
    );

    public double Angle
    {
        get => (double)GetValue(AngleProperty);
        set => SetValue(AngleProperty, value);
    }

    public Chevron()
    {
        InitializeComponent();
    }
}

public class ChevronExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new Chevron();
}
