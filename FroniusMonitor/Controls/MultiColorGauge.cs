namespace De.Hochstaetter.FroniusMonitor.Controls;

public class MultiColorGauge : ProgressBar
{
    public record ColorEntry(double Value, Color Color);

    public static readonly DependencyProperty GaugeColorsProperty = DependencyProperty.Register
    (
        nameof(GaugeColors), typeof(IReadOnlyList<ColorEntry>), typeof(MultiColorGauge),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Journal)
    );

    public IEnumerable<ColorEntry>? GaugeColors
    {
        get => (IReadOnlyList<ColorEntry>)GetValue(GaugeColorsProperty);
        set => SetValue(GaugeColorsProperty, value);
    }

    internal static DependencyPropertyKey TemplateMetadataPropertyKey = DependencyProperty.RegisterReadOnly
    (
        nameof(TemplateMetadata), typeof(object), typeof(MultiColorGauge), new FrameworkPropertyMetadata(null)
    );

    public object TemplateMetadata => GetValue(TemplateMetadataPropertyKey.DependencyProperty);

    public static readonly DependencyProperty HandFillProperty = DependencyProperty.Register
    (
        nameof(HandFill), typeof(Brush), typeof(MultiColorGauge), new FrameworkPropertyMetadata(Brushes.DarkSlateGray)
    );

    public Brush HandFill
    {
        get => (Brush)GetValue(HandFillProperty);
        set => SetValue(HandFillProperty, value);
    }

    public static readonly DependencyProperty TickFillProperty = DependencyProperty.Register
    (
        nameof(TickFill), typeof(Brush), typeof(MultiColorGauge), new FrameworkPropertyMetadata(Brushes.LightGray)
    );

    public Brush TickFill
    {
        get => (Brush)GetValue(TickFillProperty);
        set => SetValue(TickFillProperty, value);
    }
}
