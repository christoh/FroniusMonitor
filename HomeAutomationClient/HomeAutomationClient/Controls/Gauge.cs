using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using HomeAutomationClient.Models;

namespace HomeAutomationClient.Controls;

public class Gauge : ContentControl
{
    public static readonly StyledProperty<double> MinimumProperty = AvaloniaProperty.Register<Gauge, double>(nameof(Minimum), 0d);

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public static readonly StyledProperty<double> MaximumProperty = AvaloniaProperty.Register<Gauge, double>(nameof(Maximum), 1d);

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<Gauge, double>(nameof(Value), 0d);

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly StyledProperty<double> AnimatedValueProperty = AvaloniaProperty.Register<Gauge, double>(nameof(AnimatedValue), 0d);

    public double AnimatedValue
    {
        get => GetValue(AnimatedValueProperty);
        set => SetValue(AnimatedValueProperty, value);
    }

    public static readonly StyledProperty<IReadOnlyList<ColorThreshold>?> GaugeColorsProperty = AvaloniaProperty.Register<Gauge, IReadOnlyList<ColorThreshold>?>(nameof(GaugeColors));

    public IReadOnlyList<ColorThreshold>? GaugeColors
    {
        get => GetValue(GaugeColorsProperty);
        set => SetValue(GaugeColorsProperty, value);
    }

    public static readonly StyledProperty<double> OriginProperty = AvaloniaProperty.Register<Gauge, double>(nameof(Origin));

    public double Origin
    {
        get => GetValue(OriginProperty);
        set => SetValue(OriginProperty, value);
    }

    public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<Gauge, string>(nameof(Label), string.Empty);

    public string? Label
    {
        get => (string?)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value ?? string.Empty);
    }

    public static readonly StyledProperty<string> StringFormatProperty = AvaloniaProperty.Register<Gauge, string>(nameof(StringFormat), "N0");

    public string StringFormat
    {
        get => GetValue(StringFormatProperty);
        set => SetValue(StringFormatProperty, value);
    }

    public static readonly StyledProperty<string> UnitNameProperty = AvaloniaProperty.Register<Gauge, string>(nameof(UnitName), string.Empty);

    public string UnitName
    {
        get => GetValue(UnitNameProperty);
        set => SetValue(UnitNameProperty, value);
    }

    public static readonly StyledProperty<TimeSpan> AnimationDurationProperty = AvaloniaProperty.Register<Gauge, TimeSpan>(nameof(AnimationDuration), TimeSpan.FromSeconds(1));

    public TimeSpan AnimationDuration
    {
        get => GetValue(AnimationDurationProperty);
        set => SetValue(AnimationDurationProperty, value);
    }

    public static readonly StyledProperty<bool> ShowPercentProperty = AvaloniaProperty.Register<Gauge, bool>(nameof(ShowPercent));

    public bool ShowPercent
    {
        get => GetValue(ShowPercentProperty);
        set => SetValue(ShowPercentProperty, value);
    }

    internal static readonly StyledProperty<object?> TemplateMetadataProperty = AvaloniaProperty.Register<Gauge, object?>(nameof(TemplateMetadata), defaultBindingMode: BindingMode.OneWayToSource);

    public object? TemplateMetadata => GetValue(TemplateMetadataProperty);

    public static readonly StyledProperty<IBrush?> HandFillProperty = AvaloniaProperty.Register<Gauge, IBrush?>(nameof(HandFill), Brushes.DarkSlateGray);

    public IBrush? HandFill
    {
        get => GetValue(HandFillProperty);
        set => SetValue(HandFillProperty, value);
    }

    public static readonly StyledProperty<IBrush?> TickFillProperty = AvaloniaProperty.Register<Gauge, IBrush?>(nameof(TickFill), Brushes.LightGray);

    public IBrush? TickFill
    {
        get => GetValue(TickFillProperty);
        set => SetValue(TickFillProperty, value);
    }
    
    public static readonly StyledProperty<bool> ColorAllTicksProperty = AvaloniaProperty.Register<Gauge, bool>(nameof(ColorAllTicks));

    public bool ColorAllTicks
    {
        get => GetValue(ColorAllTicksProperty);
        set => SetValue(ColorAllTicksProperty, value);
    }

    public static IReadOnlyList<ColorThreshold> HighIsBad { get; } =
    [
        new(0, Colors.Green),
        new(.75, Colors.YellowGreen),
        new(.95, Colors.OrangeRed),
        new(1, Colors.Red),
    ];

    public static IReadOnlyList<ColorThreshold> MidIsGood { get; } =
    [
        new(0, Colors.Red),
        new(.05, Colors.OrangeRed),
        new(.25, Colors.YellowGreen),
        new(0.5, Colors.Green),
        new(.75, Colors.YellowGreen),
        new(.95, Colors.OrangeRed),
        new(1, Colors.Red),
    ];

    public static IReadOnlyList<ColorThreshold> ExtremeIsBad { get; } =
    [
        new(0, Colors.Red),
        new(.01, Colors.OrangeRed),
        new(.10, Colors.YellowGreen),
        new(0.20, Colors.Green),
        new(0.70, Colors.Green),
        new(.90, Colors.YellowGreen),
        new(.99, Colors.OrangeRed),
        new(1, Colors.Red),
    ];

    public static IReadOnlyList<ColorThreshold> OneThirdIsGood { get; } =
    [
        new(0, Colors.Red),
        new(.2, Colors.OrangeRed),
        new(.3, Colors.YellowGreen),
        new(1d / 3d, Colors.Green),
        new(.36333333, Colors.YellowGreen),
        new(.5, Colors.OrangeRed),
        new(1, Colors.Red),
    ];

    public static IReadOnlyList<ColorThreshold> MidIsBad { get; } =
    [
        new(0, Colors.Green),
        new(.20, Colors.YellowGreen),
        new(.3333333, Colors.OrangeRed),
        new(0.5, Colors.Red),
        new(.6666667, Colors.OrangeRed),
        new(.8, Colors.YellowGreen),
        new(1, Colors.Green),
    ];

    public static IReadOnlyList<ColorThreshold> HigherThan15IsBad { get; } =
    [
        new(0, Colors.Green),
        new(.10, Colors.YellowGreen),
        new(.15, Colors.OrangeRed),
        new(1, Colors.Red),
    ];

    public static IReadOnlyList<ColorThreshold> LowIsBad { get; } =
    [
        new(0, Colors.Red),
        new(.05, Colors.OrangeRed),
        new(.5, Colors.YellowGreen),
        new(1, Colors.Green),
    ];

    public static IReadOnlyList<ColorThreshold> VeryHighIsGood { get; } =
    [
        new(0, Colors.Red),
        new(.7, Colors.OrangeRed),
        new(.9, Colors.YellowGreen),
        new(1, Colors.Green),
    ];

    public static IReadOnlyList<ColorThreshold> VeryLowIsBad { get; } =
    [
        new(0, Colors.Red),
        new(.025, Colors.OrangeRed),
        new(.1, Colors.YellowGreen),
        new(1, Colors.Green),
    ];

    public static IReadOnlyList<ColorThreshold> AllIsGood { get; } =
    [
        new(0, Colors.Green),
        new(1, Colors.Green),
    ];

    protected Color GetColorForRelativeValue(double relativeValue)
    {
        if (GaugeColors == null || !GaugeColors.Any())
        {
            return Colors.Green;
        }

        var upper = GaugeColors.First(c => c.Soc > relativeValue || c.Soc >= 1);
        var lower = GaugeColors.Last(c => c.Soc < upper.Soc || c.Soc <= 0);

        var lowerPercentage = (float)((upper.Soc - relativeValue) / (upper.Soc - lower.Soc));

        return new Color
        (
            (byte)Math.Round(lower.Color.A * lowerPercentage + upper.Color.A * (1 - lowerPercentage)),
            (byte)Math.Round(lower.Color.R * lowerPercentage + upper.Color.R * (1 - lowerPercentage)),
            (byte)Math.Round(lower.Color.G * lowerPercentage + upper.Color.G * (1 - lowerPercentage)),
            (byte)Math.Round(lower.Color.B * lowerPercentage + upper.Color.B * (1 - lowerPercentage))
        );
    }
    
    protected double SetValue(bool skipAnimation = false)
    {
        var relativeValue = (Math.Max(Math.Min(Maximum, Value), Minimum) - Minimum) / (Maximum - Minimum);

        //if (skipAnimation)
        //{
            AnimatedValue = relativeValue;
            return relativeValue;
        //}
        
        //var animation = new Animation
        //{
        //    Duration = AnimationDuration,
        //    Easing = new LinearEasing(),
        //    FillMode = FillMode.Forward,
        //    IterationCount = new IterationCount(1),
        //    Children = { new KeyFrame{Cue = new Cue(AnimatedValue),Setters = { Capacity = 1,ResetBehavior = ResetBehavior.Remove, Validate = }} }
        //};
    }

    
}
