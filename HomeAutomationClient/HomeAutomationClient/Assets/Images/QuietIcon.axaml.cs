namespace De.Hochstaetter.HomeAutomationClient.Assets.Images;

public partial class QuietIcon : Viewbox
{
    public static readonly StyledProperty<IBrush?> FillProperty = AvaloniaProperty.Register<QuietIcon, IBrush?>(nameof(Fill), Brushes.Black);

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public static readonly StyledProperty<IBrush?> RingProperty = AvaloniaProperty.Register<QuietIcon, IBrush?>(nameof(Ring), Brushes.Black);

    public IBrush? Ring
    {
        get => GetValue(RingProperty);
        set => SetValue(RingProperty, value);
    }

    public QuietIcon()
    {
        InitializeComponent();
    }
}
