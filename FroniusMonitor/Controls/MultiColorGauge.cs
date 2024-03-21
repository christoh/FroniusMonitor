namespace De.Hochstaetter.FroniusMonitor.Controls;

public class MultiColorGauge : ProgressBar
{
    public record ColorEntry(double Value, Color Color);

    public static readonly DependencyProperty GaugeColorsProperty = DependencyProperty.Register
    (
        nameof(GaugeColors), typeof(IReadOnlyList<ColorEntry>), typeof(MultiColorGauge),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
    );

    public IEnumerable<ColorEntry>? GaugeColors
    {
        get => (IReadOnlyList<ColorEntry>)GetValue(GaugeColorsProperty);
        set => SetValue(GaugeColorsProperty, value);
    }
}
