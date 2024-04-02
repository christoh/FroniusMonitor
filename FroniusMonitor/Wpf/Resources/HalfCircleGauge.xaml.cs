using System.Windows.Shapes;

namespace De.Hochstaetter.FroniusMonitor.Wpf.Resources;

public partial class HalfCircleGauge
{
    private record GaugeMetaData(Polygon Hand, double HandLength, Canvas Canvas)
    {
        public double OldMinimum = double.MinValue;
        public double OldMaximum = double.MaxValue;
    }

    #region Dependency Properties

    private static readonly DependencyPropertyDescriptor? valueDescriptor = DependencyPropertyDescriptor.FromProperty(RangeBase.ValueProperty, typeof(MultiColorGauge));
    private static readonly DependencyPropertyDescriptor? minimumDescriptor = DependencyPropertyDescriptor.FromProperty(RangeBase.MinimumProperty, typeof(MultiColorGauge));
    private static readonly DependencyPropertyDescriptor? maximumDescriptor = DependencyPropertyDescriptor.FromProperty(RangeBase.MaximumProperty, typeof(MultiColorGauge));
    private static readonly DependencyPropertyDescriptor? colorsDescriptor = DependencyPropertyDescriptor.FromProperty(MultiColorGauge.GaugeColorsProperty, typeof(MultiColorGauge));
    private static readonly DependencyPropertyDescriptor? handFillDescriptor = DependencyPropertyDescriptor.FromProperty(MultiColorGauge.HandFillProperty, typeof(MultiColorGauge));
    private static readonly DependencyPropertyDescriptor? tickFillDescriptor = DependencyPropertyDescriptor.FromProperty(MultiColorGauge.TickFillProperty, typeof(MultiColorGauge));
    private static readonly DependencyPropertyDescriptor? colorAllTicksDescriptor = DependencyPropertyDescriptor.FromProperty(MultiColorGauge.ColorAllTicksProperty, typeof(MultiColorGauge));

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

    public static readonly DependencyProperty MinimumMaximumStringFormatProperty = DependencyProperty.RegisterAttached
    (
        nameof(GetMinimumMaximumStringFormat)[3..], typeof(string), typeof(HalfCircleGauge), new FrameworkPropertyMetadata("N0")
    );

    public static string GetMinimumMaximumStringFormat(DependencyObject element)
    {
        return (string)element.GetValue(MinimumMaximumStringFormatProperty);
    }

    public static void SetMinimumMaximumStringFormat(DependencyObject element, string value)
    {
        element.SetValue(MinimumMaximumStringFormatProperty, value);
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

        var (gauge, canvas, outerCanvas, minimumTextBlock, maximumTextBlock) = GetGaugeAndCanvas(element);
        var handLength = outerCanvas.Width * 0.45;

        var hand = new Polygon
        {
            Points = new PointCollection(
            [
                new Point(4, 0), new Point(0, handLength), new Point(8, handLength)
            ]),

            Fill = gauge.HandFill,
            StrokeThickness = 0,
            RenderTransform = new RotateTransform(-90, 4, handLength - 5),
        };

        hand.SetValue(Canvas.BottomProperty, 8d);
        hand.SetValue(Canvas.LeftProperty, outerCanvas.Width / 2 - 4);
        outerCanvas.Children.Add(hand);
        gauge.SetValue(MultiColorGauge.TemplateMetadataPropertyKey, new GaugeMetaData(hand, handLength, canvas));

        valueDescriptor?.AddValueChanged(gauge, (_, _) => SetValue(gauge));
        minimumDescriptor?.AddValueChanged(gauge, (_, _) => OnMinimumMaximumChanged(gauge, minimumTextBlock, maximumTextBlock));
        maximumDescriptor?.AddValueChanged(gauge, (_, _) => OnMinimumMaximumChanged(gauge, minimumTextBlock, maximumTextBlock));
        colorsDescriptor?.AddValueChanged(gauge, (_, _) => SetValue(gauge));
        handFillDescriptor?.AddValueChanged(gauge, OnHandBrushChanged);
        tickFillDescriptor?.AddValueChanged(gauge, (_, _) => OnParametersChanged(gauge, minimumTextBlock, maximumTextBlock));

        colorAllTicksDescriptor?.AddValueChanged(gauge, (_, _) =>
        {
            OnParametersChanged(gauge, minimumTextBlock, maximumTextBlock);

            if (!gauge.ColorAllTicks)
            {
                var relativeValue=SetValue(gauge, true);
                OnAnimatedAngleChanged(gauge, new DependencyPropertyChangedEventArgs(AnimatedValueProperty, relativeValue, relativeValue));
            }
        });

        OnParametersChanged(gauge, minimumTextBlock, maximumTextBlock);
    }

    private static void OnMinimumMaximumChanged(MultiColorGauge gauge, TextBlock? minimumTextBlock, TextBlock? maximumTextBlock, bool skipAnimation = false)
    {
        if (gauge.TemplateMetadata is not GaugeMetaData metaData)
        {
            return;
        }

        var needsChange = false;

        if (minimumTextBlock != null && !metaData.OldMinimum.Equals(gauge.Minimum))
        {
            minimumTextBlock.Text = gauge.Minimum.ToString(GetMinimumMaximumStringFormat(gauge), CultureInfo.CurrentCulture);
            metaData.OldMinimum = gauge.Minimum;
            needsChange = true;
        }

        if (maximumTextBlock != null && !metaData.OldMaximum.Equals(gauge.Maximum))
        {
            maximumTextBlock.Text = gauge.Maximum.ToString(GetMinimumMaximumStringFormat(gauge), CultureInfo.CurrentCulture);
            metaData.OldMaximum = gauge.Maximum;
            needsChange = true;
        }

        if (needsChange)
        {
            SetValue(gauge, skipAnimation);
        }
    }

    private static double SetValue(MultiColorGauge gauge, bool skipAnimation = false)
    {
        var relativeValue = (Math.Max(Math.Min(gauge.Maximum, gauge.Value), gauge.Minimum) - gauge.Minimum) / (gauge.Maximum - gauge.Minimum);

        var animation = skipAnimation
            ? null
            : new DoubleAnimation(double.IsFinite(relativeValue) ? relativeValue : 0, gauge.AnimationDuration)
            {
                AccelerationRatio = .33,
                DecelerationRatio = .33,
            };

        gauge.BeginAnimation(AnimatedValueProperty, animation);

        if (skipAnimation)
        {
            SetAnimatedValue(gauge, relativeValue);
        }

        return relativeValue;
    }

    private static void OnAnimatedAngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var gauge = (MultiColorGauge)d;
        var (hand, handLength, canvas) = (GaugeMetaData)gauge.TemplateMetadata;
        var relativeValue = (double)e.NewValue;
        hand.RenderTransform = new RotateTransform((relativeValue - 0.5) * 180, 4, handLength - 5);

        if (!gauge.ColorAllTicks)
        {
            canvas.Children.OfType<Rectangle>().Apply(rect => ColorShape(gauge, rect, relativeValue));
        }
    }

    private static void ColorShape(MultiColorGauge gauge, Shape rect, double relativeValue)
    {
        var rectRelativeValue = Math.Round((double)rect.Tag, 6);

        if
        (
            gauge.ColorAllTicks ||
            relativeValue < rectRelativeValue && rectRelativeValue <= gauge.Origin ||
            relativeValue > rectRelativeValue && rectRelativeValue >= gauge.Origin
        )
        {
            var brush = new SolidColorBrush(MultiColorGauge.GetColorForRelativeValue(gauge, rectRelativeValue));
            brush.Freeze();
            rect.Fill = brush;
            return;
        }

        rect.Fill = gauge.TickFill;
    }

    private static void OnParametersChanged(MultiColorGauge gauge, TextBlock? minimumTextBlock, TextBlock? maximumTextBlock)
    {
        var (_, _, canvas) = (GaugeMetaData)gauge.TemplateMetadata;

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
            OnMinimumMaximumChanged(gauge, minimumTextBlock, maximumTextBlock, true);

            if (gauge.ColorAllTicks)
            {
                canvas.Children.OfType<Rectangle>().Apply(rect => ColorShape(gauge, rect, 0));
            }
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

    private static (MultiColorGauge Gauge, Canvas Canvas, Canvas OuterCanvas, TextBlock MinimumTextBlock, TextBlock MaximumTextBlock) GetGaugeAndCanvas(object? sender)
    {
        if
        (
            sender is MultiColorGauge gauge &&
            gauge.Template.FindName("Canvas", gauge) is Canvas canvas &&
            canvas.FindName("OuterCanvas") is Canvas outerCanvas &&
            canvas.FindName("MinimumTextBlock") is TextBlock minimumTextBlock &&
            canvas.FindName("MaximumTextBlock") is TextBlock maximumTextBlock
        )
        {
            return (gauge, canvas, outerCanvas, minimumTextBlock, maximumTextBlock);
        }

        throw new InvalidOperationException("Required elements not found");
    }
}
