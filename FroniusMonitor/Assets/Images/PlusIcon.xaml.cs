namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class PlusIcon
{
    public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register
    (
        nameof(Foreground), typeof(Brush), typeof(PlusIcon),
        new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender)
    );

    public Brush Foreground
    {
        get => (Brush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public PlusIcon()
    {
        InitializeComponent();
    }

    public class PlusIconExtension : MarkupExtension
    {
        public Brush Foreground { get; set; } = Brushes.Black;
        public override object ProvideValue(IServiceProvider serviceProvider) => new PlusIcon { Foreground = Foreground };
    }
}
