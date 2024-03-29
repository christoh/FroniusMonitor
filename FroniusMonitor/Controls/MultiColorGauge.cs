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

    public static readonly DependencyProperty ColorAllTicksProperty = DependencyProperty.Register
    (
        nameof(ColorAllTicks), typeof(bool), typeof(MultiColorGauge)
    );

    public bool ColorAllTicks
    {
        get => (bool)GetValue(ColorAllTicksProperty);
        set => SetValue(ColorAllTicksProperty, value);
    }
    
    public static IReadOnlyList<ColorEntry> HighIsBad { get; } =
    [
        new ColorEntry(0, Colors.Green),
        new ColorEntry(.75, Colors.YellowGreen),
        new ColorEntry(.95, Colors.OrangeRed),
        new ColorEntry(1, Colors.Red),
    ];

    public static IReadOnlyList<ColorEntry> MidIsGood { get; } =
    [
        new ColorEntry(0, Colors.Red),
        new ColorEntry(.05, Colors.OrangeRed),
        new ColorEntry(.25, Colors.YellowGreen),
        new ColorEntry(0.5,Colors.Green),
        new ColorEntry(.75, Colors.YellowGreen),
        new ColorEntry(.95, Colors.OrangeRed),
        new ColorEntry(1, Colors.Red),
    ];

    public static IReadOnlyList<ColorEntry> MidIsBad { get; } =
    [
        new ColorEntry(0, Colors.Green),
        new ColorEntry(.05, Colors.YellowGreen),
        new ColorEntry(.25, Colors.OrangeRed),
        new ColorEntry(0.5,Colors.Red),
        new ColorEntry(.75, Colors.OrangeRed),
        new ColorEntry(.95, Colors.YellowGreen),
        new ColorEntry(1, Colors.Green),
    ];

    public static IReadOnlyList<MultiColorGauge.ColorEntry> LowIsBad { get; } =
    [
        new MultiColorGauge.ColorEntry(0,Colors.Red),
        new MultiColorGauge.ColorEntry(.05,Colors.OrangeRed),
        new MultiColorGauge.ColorEntry(.5,Colors.YellowGreen),
        new MultiColorGauge.ColorEntry(1,Colors.Green),
    ];

    static MultiColorGauge()
    {
        ValueProperty.OverrideMetadata(typeof(MultiColorGauge), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender, (d, e) => { }, (_, x) => x));
    }

    internal static Color GetColorForRelativeValue(MultiColorGauge gauge, double relativeValue)
    {
        if (gauge.GaugeColors == null || !gauge.GaugeColors.Any())
        {
            return Colors.Green;
        }

        var upper = gauge.GaugeColors.First(c => c.Value > relativeValue || c.Value >= 1);
        var lower = gauge.GaugeColors.Last(c => c.Value < upper.Value || c.Value <= 0);

        var lowerPercentage = (upper.Value - relativeValue) / (upper.Value - lower.Value);

        return Color.FromRgb
        (
            (byte)Math.Round(lowerPercentage * lower.Color.R + (1 - lowerPercentage) * upper.Color.R, MidpointRounding.AwayFromZero),
            (byte)Math.Round(lowerPercentage * lower.Color.G + (1 - lowerPercentage) * upper.Color.G, MidpointRounding.AwayFromZero),
            (byte)Math.Round(lowerPercentage * lower.Color.B + (1 - lowerPercentage) * upper.Color.B, MidpointRounding.AwayFromZero)
        );
    }
}
