﻿using System.Windows.Shapes;

namespace De.Hochstaetter.FroniusMonitor.Wpf.Resources;

public partial class HalfCircleGauge
{
    // ReSharper disable once NotAccessedPositionalProperty.Local
    private record GaugeMetaData(Polygon Hand, double HandLength, Canvas Canvas, Canvas OuterCanvas);

    #region Dependency Properties

    private readonly DependencyPropertyDescriptor? valueDescriptor = DependencyPropertyDescriptor.FromProperty(RangeBase.ValueProperty, typeof(MultiColorGauge));
    private readonly DependencyPropertyDescriptor? minimumDescriptor = DependencyPropertyDescriptor.FromProperty(RangeBase.MinimumProperty, typeof(MultiColorGauge));
    private readonly DependencyPropertyDescriptor? maximumDescriptor = DependencyPropertyDescriptor.FromProperty(RangeBase.MaximumProperty, typeof(MultiColorGauge));
    private readonly DependencyPropertyDescriptor? colorsDescriptor = DependencyPropertyDescriptor.FromProperty(MultiColorGauge.GaugeColorsProperty, typeof(MultiColorGauge));
    private readonly DependencyPropertyDescriptor? handFillDescriptor = DependencyPropertyDescriptor.FromProperty(MultiColorGauge.HandFillProperty, typeof(MultiColorGauge));
    private readonly DependencyPropertyDescriptor? tickFillDescriptor = DependencyPropertyDescriptor.FromProperty(MultiColorGauge.TickFillProperty, typeof(MultiColorGauge));

    public static readonly DependencyProperty AnimatedValueProperty = DependencyProperty.RegisterAttached
    (
        nameof(GetAnimatedValue)[3..], typeof(double), typeof(MultiColorGauge),
        new PropertyMetadata(OnAnimatedAngleChanged)
    );

    public static double GetAnimatedValue(DependencyObject element)
    {
        return (double)element.GetValue(AnimatedValueProperty);
    }

    public static void SetAnimatedValue(DependencyObject element, double value)
    {
        element.SetValue(AnimatedValueProperty, value);
    }

    #endregion

    public HalfCircleGauge()
    {
        InitializeComponent();
    }

    private void OnTemplateLoaded(object sender, EventArgs? _ = null)
    {
        if (sender is not FrameworkElement { TemplatedParent: FrameworkElement element })
        {
            return;
        }

        var (gauge, canvas, outerCanvas) = GetGaugeAndCanvas(element);
        var handLength = outerCanvas.Width * 0.45;

        var hand = new Polygon
        {
            Points = new PointCollection([new Point(4, 0), new Point(0, handLength), new Point(8, handLength)]),
            Fill = gauge.HandFill,
            StrokeThickness = 0,
            RenderTransform = new RotateTransform(-90, 4, handLength - 5),
        };

        hand.SetValue(Canvas.BottomProperty, 8d);
        hand.SetValue(Canvas.LeftProperty, outerCanvas.Width / 2 - 4);
        outerCanvas.Children.Add(hand);
        gauge.SetValue(MultiColorGauge.TemplateMetadataPropertyKey, new GaugeMetaData(hand, handLength, canvas, outerCanvas));

        valueDescriptor?.AddValueChanged(gauge, OnValueChanged);
        minimumDescriptor?.AddValueChanged(gauge, OnValueChanged);
        maximumDescriptor?.AddValueChanged(gauge, OnValueChanged);
        colorsDescriptor?.AddValueChanged(gauge, OnValueChanged);
        handFillDescriptor?.AddValueChanged(gauge, OnHandBrushChanged);
        tickFillDescriptor?.AddValueChanged(gauge, OnParametersChanged);
        OnParametersChanged(gauge);
    }

    private static void OnValueChanged(object? sender, EventArgs? _ = null)
    {
        if (sender is MultiColorGauge gauge)
        {
            SetValue(gauge);
        }
    }

    private static void SetValue(RangeBase gauge, bool skipAnimation = false)
    {
        var relativeValue = Math.Max(Math.Min(gauge.Maximum, gauge.Value), gauge.Minimum) / (gauge.Maximum - gauge.Minimum);

        var animation = skipAnimation
            ? null
            : new DoubleAnimation(double.IsFinite(relativeValue) ? relativeValue : 0, TimeSpan.FromSeconds(.5))
            {
                AccelerationRatio = .2,
                DecelerationRatio = .2,
            };

        gauge.BeginAnimation(AnimatedValueProperty, animation);

        if (skipAnimation)
        {
            SetAnimatedValue(gauge, relativeValue);
        }
    }

    private static void OnAnimatedAngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var gauge = (MultiColorGauge)d;
        var (hand, handLength, canvas, _) = (GaugeMetaData)gauge.TemplateMetadata;
        var relativeValue = (double)e.NewValue;
        hand.RenderTransform = new RotateTransform((relativeValue - 0.5) * 180, 4, handLength - 5);
        canvas.Children.OfType<Rectangle>().Apply(rect => ColorShape(gauge, rect, relativeValue));
    }

    private static void ColorShape(MultiColorGauge gauge, Shape rect, double relativeValue)
    {
        var rectRelativeValue = Math.Round((double)rect.Tag, 6);

        if (rectRelativeValue > relativeValue || relativeValue <= 0 || gauge.GaugeColors == null)
        {
            rect.Fill = gauge.TickFill;
            return;
        }

        var upper = gauge.GaugeColors.First(c => c.Value > rectRelativeValue || c.Value >= 1);
        var lower = gauge.GaugeColors.Last(c => c.Value < upper.Value || c.Value <= 0);

        var lowerPercentage = (upper.Value - rectRelativeValue) / (upper.Value - lower.Value);

        var color = Color.FromRgb
        (
            (byte)Math.Round(lowerPercentage * lower.Color.R + (1 - lowerPercentage) * upper.Color.R, MidpointRounding.AwayFromZero),
            (byte)Math.Round(lowerPercentage * lower.Color.G + (1 - lowerPercentage) * upper.Color.G, MidpointRounding.AwayFromZero),
            (byte)Math.Round(lowerPercentage * lower.Color.B + (1 - lowerPercentage) * upper.Color.B, MidpointRounding.AwayFromZero)
        );

        var brush = new SolidColorBrush(color);
        brush.Freeze();
        rect.Fill = brush;
    }

    private static void OnParametersChanged(object? sender, EventArgs? _ = null)
    {
        if (sender is not MultiColorGauge gauge)
        {
            return;
        }

        var (_, _, canvas, _) = (GaugeMetaData)gauge.TemplateMetadata;

        try
        {
            canvas.Children.OfType<Rectangle>().ToList().Apply(canvas.Children.Remove);
            const double height = 5;
            const double width = 18;

            for (double angle = 0; angle < 180.0001; angle += 180d / 26d)
            {
                var (sin, cos) = Math.SinCos(angle / (180 / Math.PI));
                var translationLength = canvas.Width / 2 - width / 2;

                var rect = new Rectangle
                {
                    Height = height,
                    Width = width,
                    Fill = gauge.TickFill,
                    StrokeThickness = 0,
                    RenderTransform = new RotateTransform(angle, width / 2, height / 2),
                    Tag = angle / 180,
                };

                rect.SetValue(Canvas.BottomProperty, sin * translationLength);
                rect.SetValue(Canvas.RightProperty, cos * translationLength + canvas.Width / 2 - width / 2);
                canvas.Children.Add(rect);
            }
        }
        finally
        {
            SetValue(gauge, true);
        }
    }

    private static void OnHandBrushChanged(object? sender, EventArgs? _ = null)
    {
        if (sender is not MultiColorGauge { TemplateMetadata: GaugeMetaData metaData } gauge)
        {
            return;
        }

        metaData.Hand.Fill = gauge.HandFill;
    }

    private static (MultiColorGauge Gauge, Canvas Canvas, Canvas OuterCanvas) GetGaugeAndCanvas(object? sender)
    {
        if
        (
            sender is MultiColorGauge gauge &&
            gauge.Template.FindName("Canvas", gauge) is Canvas canvas &&
            gauge.Template.FindName("OuterCanvas", gauge) is Canvas outerCanvas
        )
        {
            return (gauge, canvas, outerCanvas);
        }

        throw new InvalidOperationException("Must define Canvas and OuterCanvas");
    }
}
