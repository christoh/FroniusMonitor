namespace De.Hochstaetter.FroniusMonitor.Controls;

public class Gauge : ProgressBar
{
    public static readonly DependencyProperty GaugeColorsProperty = DependencyProperty.Register
    (
        nameof(GaugeColors), typeof(IReadOnlyList<ColorThreshold>), typeof(Gauge),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Journal)
    );

    public IEnumerable<ColorThreshold>? GaugeColors
    {
        get => (IReadOnlyList<ColorThreshold>)GetValue(GaugeColorsProperty);
        set => SetValue(GaugeColorsProperty, value);
    }

    public static readonly DependencyProperty OriginProperty = DependencyProperty.Register
    (
        nameof(Origin), typeof(double), typeof(Gauge),
        new FrameworkPropertyMetadata(0.0)
    );

    public double Origin
    {
        get => (double)GetValue(OriginProperty);
        set => SetValue(OriginProperty, value);
    }

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register
    (
        nameof(Label), typeof(string), typeof(Gauge), new FrameworkPropertyMetadata(string.Empty)
    );

    public string? Label
    {
        get => (string?)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value ?? string.Empty);
    }

    public static readonly DependencyProperty StringFormatProperty = DependencyProperty.Register
    (
        nameof(StringFormat), typeof(string), typeof(Gauge),
        new FrameworkPropertyMetadata("N1")
    );

    public string StringFormat
    {
        get => (string)GetValue(StringFormatProperty);
        set => SetValue(StringFormatProperty, value);
    }

    public static readonly DependencyProperty UnitNameProperty = DependencyProperty.Register
    (
        nameof(UnitName), typeof(string), typeof(Gauge),
        new PropertyMetadata(string.Empty)
    );

    public string UnitName
    {
        get => (string)GetValue(UnitNameProperty);
        set => SetValue(UnitNameProperty, value);
    }

    public static readonly DependencyProperty AnimationDurationProperty = DependencyProperty.Register
    (
        nameof(AnimationDuration), typeof(TimeSpan), typeof(Gauge),
        new PropertyMetadata(TimeSpan.FromSeconds(.5))
    );

    public TimeSpan AnimationDuration
    {
        get => (TimeSpan)GetValue(AnimationDurationProperty);
        set => SetValue(AnimationDurationProperty, value);
    }

    public static readonly DependencyProperty ShowPercentProperty = DependencyProperty.Register
    (
        nameof(ShowPercent), typeof(bool), typeof(Gauge)
    );

    public bool ShowPercent
    {
        get => (bool)GetValue(ShowPercentProperty);
        set => SetValue(ShowPercentProperty, value);
    }

    internal static DependencyPropertyKey TemplateMetadataPropertyKey = DependencyProperty.RegisterReadOnly
    (
        nameof(TemplateMetadata), typeof(object), typeof(Gauge), new FrameworkPropertyMetadata(null)
    );

    public object TemplateMetadata => GetValue(TemplateMetadataPropertyKey.DependencyProperty);

    public static readonly DependencyProperty HandFillProperty = DependencyProperty.Register
    (
        nameof(HandFill), typeof(Brush), typeof(Gauge), new FrameworkPropertyMetadata(Brushes.DarkSlateGray)
    );

    public Brush HandFill
    {
        get => (Brush)GetValue(HandFillProperty);
        set => SetValue(HandFillProperty, value);
    }

    public static readonly DependencyProperty TickFillProperty = DependencyProperty.Register
    (
        nameof(TickFill), typeof(Brush), typeof(Gauge), new FrameworkPropertyMetadata(Brushes.LightGray)
    );

    public Brush TickFill
    {
        get => (Brush)GetValue(TickFillProperty);
        set => SetValue(TickFillProperty, value);
    }

    public static readonly DependencyProperty ColorAllTicksProperty = DependencyProperty.Register
    (
        nameof(ColorAllTicks), typeof(bool), typeof(Gauge)
    );

    public bool ColorAllTicks
    {
        get => (bool)GetValue(ColorAllTicksProperty);
        set => SetValue(ColorAllTicksProperty, value);
    }

    public static IReadOnlyList<ColorThreshold> HighIsBad { get; } =
    [
        new ColorThreshold(0, Colors.Green),
        new ColorThreshold(.75, Colors.YellowGreen),
        new ColorThreshold(.95, Colors.OrangeRed),
        new ColorThreshold(1, Colors.Red),
    ];

    public static IReadOnlyList<ColorThreshold> MidIsGood { get; } =
    [
        new ColorThreshold(0, Colors.Red),
        new ColorThreshold(.05, Colors.OrangeRed),
        new ColorThreshold(.25, Colors.YellowGreen),
        new ColorThreshold(0.5, Colors.Green),
        new ColorThreshold(.75, Colors.YellowGreen),
        new ColorThreshold(.95, Colors.OrangeRed),
        new ColorThreshold(1, Colors.Red),
    ];

    public static IReadOnlyList<ColorThreshold> ExtremeIsBad { get; } =
    [
        new ColorThreshold(0, Colors.Red),
        new ColorThreshold(.01, Colors.OrangeRed),
        new ColorThreshold(.10, Colors.YellowGreen),
        new ColorThreshold(0.20, Colors.Green),
        new ColorThreshold(0.70, Colors.Green),
        new ColorThreshold(.90, Colors.YellowGreen),
        new ColorThreshold(.99, Colors.OrangeRed),
        new ColorThreshold(1, Colors.Red),
    ];

    public static IReadOnlyList<ColorThreshold> OneThirdIsGood { get; } =
    [
        new ColorThreshold(0, Colors.Red),
        new ColorThreshold(.2, Colors.OrangeRed),
        new ColorThreshold(.3, Colors.YellowGreen),
        new ColorThreshold(1d / 3d, Colors.Green),
        new ColorThreshold(.36333333, Colors.YellowGreen),
        new ColorThreshold(.5, Colors.OrangeRed),
        new ColorThreshold(1, Colors.Red),
    ];

    public static IReadOnlyList<ColorThreshold> MidIsBad { get; } =
    [
        new ColorThreshold(0, Colors.Green),
        new ColorThreshold(.20, Colors.YellowGreen),
        new ColorThreshold(.3333333, Colors.OrangeRed),
        new ColorThreshold(0.5, Colors.Red),
        new ColorThreshold(.6666667, Colors.OrangeRed),
        new ColorThreshold(.8, Colors.YellowGreen),
        new ColorThreshold(1, Colors.Green),
    ];

    public static IReadOnlyList<ColorThreshold> HigherThan15IsBad { get; } =
    [
        new ColorThreshold(0, Colors.Green),
        new ColorThreshold(.10, Colors.YellowGreen),
        new ColorThreshold(.15, Colors.OrangeRed),
        new ColorThreshold(1, Colors.Red),
    ];

    public static IReadOnlyList<ColorThreshold> LowIsBad { get; } =
    [
        new ColorThreshold(0, Colors.Red),
        new ColorThreshold(.05, Colors.OrangeRed),
        new ColorThreshold(.5, Colors.YellowGreen),
        new ColorThreshold(1, Colors.Green),
    ];
    
    public static IReadOnlyList<ColorThreshold> VeryHighIsGood { get; } =
    [
        new ColorThreshold(0, Colors.Red),
        new ColorThreshold(.7, Colors.OrangeRed),
        new ColorThreshold(.9, Colors.YellowGreen),
        new ColorThreshold(1, Colors.Green),
    ];

    public static IReadOnlyList<ColorThreshold> VeryLowIsBad { get; } =
    [
        new ColorThreshold(0, Colors.Red),
        new ColorThreshold(.025, Colors.OrangeRed),
        new ColorThreshold(.1, Colors.YellowGreen),
        new ColorThreshold(1, Colors.Green),
    ];

    public static IReadOnlyList<ColorThreshold> AllIsGood { get; } =
    [
        new ColorThreshold(0, Colors.Green),
        new ColorThreshold(1, Colors.Green),
    ];

    static Gauge()
    {
        ValueProperty.OverrideMetadata(typeof(Gauge), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender, (_, _) => { }, (_, x) => x));
    }

    internal static Color GetColorForRelativeValue(Gauge gauge, double relativeValue)
    {
        if (gauge.GaugeColors == null || !gauge.GaugeColors.Any())
        {
            return Colors.Green;
        }

        var upper = gauge.GaugeColors.First(c => c.Soc > relativeValue || c.Soc >= 1);
        var lower = gauge.GaugeColors.Last(c => c.Soc < upper.Soc || c.Soc <= 0);

        var lowerPercentage = (float)((upper.Soc - relativeValue) / (upper.Soc - lower.Soc));
        return lower.Color * lowerPercentage + upper.Color * (1 - lowerPercentage);
    }
}

public class DemoGaugeExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new Gauge
        {
            Style = new StaticResourceExtension("DefaultHalfCircleGauge").ProvideValue(serviceProvider) as Style ?? throw new NullReferenceException("Style 'DefaultHalfCircleGauge' does not exist"),
            Minimum = 0,
            Maximum = 100,
            Value = 33,
            ColorAllTicks = true,
            GaugeColors = Gauge.HighIsBad,
        };
    }
}

