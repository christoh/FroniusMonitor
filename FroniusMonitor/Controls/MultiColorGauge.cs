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

    public static readonly DependencyProperty OriginProperty = DependencyProperty.Register
    (
        nameof(Origin), typeof(double), typeof(MultiColorGauge),
        new FrameworkPropertyMetadata(0.0)
    );

    public double Origin
    {
        get => (double)GetValue(OriginProperty);
        set => SetValue(OriginProperty, value);
    }

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register
    (
        nameof(Label), typeof(string), typeof(MultiColorGauge), new FrameworkPropertyMetadata(string.Empty)
    );

    public string? Label
    {
        get => (string?)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value ?? string.Empty);
    }

    public static readonly DependencyProperty ShowPercentProperty = DependencyProperty.Register
    (
        nameof(ShowPercent), typeof(bool), typeof(MultiColorGauge)
    );

    public bool ShowPercent
    {
        get => (bool)GetValue(ShowPercentProperty);
        set => SetValue(ShowPercentProperty, value);
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
        new ColorEntry(0.5, Colors.Green),
        new ColorEntry(.75, Colors.YellowGreen),
        new ColorEntry(.95, Colors.OrangeRed),
        new ColorEntry(1, Colors.Red),
    ];

    public static IReadOnlyList<ColorEntry> OneThirdIsGood { get; } =
    [
        new ColorEntry(0, Colors.Red),
        new ColorEntry(.2, Colors.OrangeRed),
        new ColorEntry(.3, Colors.YellowGreen),
        new ColorEntry(1d/3d, Colors.Green),
        new ColorEntry(.36333333, Colors.YellowGreen),
        new ColorEntry(.5, Colors.OrangeRed),
        new ColorEntry(1, Colors.Red),
    ];

    public static IReadOnlyList<ColorEntry> MidIsBad { get; } =
    [
        new ColorEntry(0, Colors.Green),
        new ColorEntry(.05, Colors.YellowGreen),
        new ColorEntry(.25, Colors.OrangeRed),
        new ColorEntry(0.5, Colors.Red),
        new ColorEntry(.75, Colors.OrangeRed),
        new ColorEntry(.95, Colors.YellowGreen),
        new ColorEntry(1, Colors.Green),
    ];

    public static IReadOnlyList<ColorEntry> LowIsBad { get; } =
    [
        new ColorEntry(0, Colors.Red),
        new ColorEntry(.05, Colors.OrangeRed),
        new ColorEntry(.5, Colors.YellowGreen),
        new ColorEntry(1, Colors.Green),
    ];

    public static IReadOnlyList<ColorEntry> AllIsGood { get; } =
    [
        new ColorEntry(0, Colors.Green),
        new ColorEntry(1, Colors.Green),
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
