﻿using System.Threading;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Data;
using Avalonia.Styling;
using De.Hochstaetter.HomeAutomationClient.Extensions;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

public class Gauge : ContentControl
{
    public static readonly StyledProperty<double> MinimumProperty = AvaloniaProperty.Register<Gauge, double>(nameof(Minimum));

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

    public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<Gauge, double>(nameof(Value));

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly StyledProperty<Easing> AnimationEasingProperty = AvaloniaProperty.Register<Gauge, Easing>(nameof(AnimationEasing), new CubicEaseOut());
    
    public Easing AnimationEasing
    {
        get => GetValue(AnimationEasingProperty);
        set => SetValue(AnimationEasingProperty, value);
    }

    public static readonly StyledProperty<double> AnimatedValueProperty = AvaloniaProperty.Register<Gauge, double>(nameof(AnimatedValue));

    public double AnimatedValue
    {
        get => GetValue(AnimatedValueProperty);
        set => SetValue(AnimatedValueProperty, value);
    }

    public static readonly StyledProperty<IEnumerable<ColorThreshold>> GaugeColorsProperty = AvaloniaProperty.Register<Gauge, IEnumerable<ColorThreshold>>(nameof(GaugeColors),Misc.GaugeColors.HighIsBad);

    public IEnumerable<ColorThreshold> GaugeColors
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

    public static readonly StyledProperty<IBrush?> TickFillProperty = AvaloniaProperty.Register<Gauge, IBrush?>(nameof(TickFill), Brushes.LightGray);

    public IBrush? TickFill
    {
        get => GetValue(TickFillProperty);
        set => SetValue(TickFillProperty, value);
    }

    protected Color GetColorForRelativeValue(double relativeValue)
    {
        if (GaugeColors == null || !GaugeColors.Any())
        {
            return Colors.Green;
        }

        var upper = GaugeColors.First(c => c.RelativeValue > relativeValue || c.RelativeValue >= 1);
        var lower = GaugeColors.Last(c => c.RelativeValue < upper.RelativeValue || c.RelativeValue <= 0);

        var lowerPercentage = (float)((upper.RelativeValue - relativeValue) / (upper.RelativeValue - lower.RelativeValue));
        return upper.Color.MixWith(lower.Color, lowerPercentage);
    }

    private CancellationTokenSource? animationTokenSource;

    // ReSharper disable once AsyncVoidMethod
    protected async void SetValue(bool sKipAnimation = false)
    {
        var relativeValue = (Math.Max(Math.Min(Maximum, Value), Minimum) - Minimum) / (Maximum - Minimum);

        try
        {
            if (sKipAnimation)
            {
                AnimatedValue = relativeValue;
                return;
            }

            if (animationTokenSource is { IsCancellationRequested: false })
            {
                //DO NOT use CancelAsync here, as it will not cancel the animation
                // ReSharper disable once MethodHasAsyncOverload
                animationTokenSource.Cancel();
            }

            animationTokenSource?.Dispose();
            animationTokenSource = new CancellationTokenSource();

            var animation = new Animation
            {
                Duration = AnimationDuration,
                Easing = AnimationEasing,
                IterationCount = new IterationCount(1),
                Children =
                {
                    new KeyFrame { Cue = new Cue(0), Setters = { new Setter { Property = AnimatedValueProperty, Value = AnimatedValue }, } },
                    new KeyFrame { Cue = new Cue(1), Setters = { new Setter { Property = AnimatedValueProperty, Value = relativeValue }, } },
                }
            };

            AnimatedValue = relativeValue;
            await animation.RunAsync(this, animationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // Animation was cancelled
        }
        catch
        {
            // Handle other exceptions if necessary
        }
        finally
        {
            animationTokenSource?.Dispose();
            animationTokenSource = null;
        }
    }
}
