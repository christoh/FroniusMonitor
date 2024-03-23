using System.Windows.Shapes;

namespace De.Hochstaetter.FroniusMonitor.Wpf.Resources;

public partial class HalfCircleGauge
{
    internal record GaugeMetaData(HalfCircleGauge HalfCircleGauge, Polygon Hand, double HandLength, Canvas Canvas, Canvas OuterCanvas);

    private readonly DependencyPropertyDescriptor? valueDescriptor = DependencyPropertyDescriptor.FromProperty(RangeBase.ValueProperty, typeof(MultiColorGauge));
    private readonly DependencyPropertyDescriptor? minimumDescriptor = DependencyPropertyDescriptor.FromProperty(RangeBase.MinimumProperty, typeof(MultiColorGauge));
    private readonly DependencyPropertyDescriptor? maximumDescriptor = DependencyPropertyDescriptor.FromProperty(RangeBase.MaximumProperty, typeof(MultiColorGauge));
    private readonly DependencyPropertyDescriptor? colorsDescriptor = DependencyPropertyDescriptor.FromProperty(MultiColorGauge.GaugeColorsProperty, typeof(MultiColorGauge));

    public static readonly DependencyProperty AnimatedValueProperty = DependencyProperty.RegisterAttached
    (
        "AnimatedValue", typeof(double), typeof(MultiColorGauge),
        new PropertyMetadata(OnAnimatedAngleChanged)
    );

    public static double GetAnimatedValue(UIElement element)
    {
        return (double)element.GetValue(AnimatedValueProperty);
    }

    public static void SetAnimatedValue(UIElement element, double value)
    {
        element.SetValue(AnimatedValueProperty, value);
    }

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
            Fill = Brushes.Black,
            StrokeThickness = 0,
            RenderTransform = new RotateTransform(-90, 4, handLength - 5),
        };

        hand.SetValue(Canvas.BottomProperty, 8d);
        hand.SetValue(Canvas.LeftProperty, outerCanvas.Width / 2 - 4);
        outerCanvas.Children.Add(hand);
        gauge.SetValue(MultiColorGauge.TemplateMetadataPropertyKey, new GaugeMetaData(this, hand, handLength, canvas, outerCanvas));

        valueDescriptor?.AddValueChanged(gauge, OnValueChanged);
        minimumDescriptor?.AddValueChanged(gauge, OnValueChanged);
        maximumDescriptor?.AddValueChanged(gauge, OnValueChanged);
        colorsDescriptor?.AddValueChanged(gauge, OnValueChanged);
        OnParametersChanged(gauge);
    }

    private static void OnValueChanged(object? sender, EventArgs? _ = null)
    {
        if (sender is MultiColorGauge gauge)
        {
            SetValue(gauge);
        }
    }

    private static void SetValue(RangeBase gauge)
    {
        var relativeValue = Math.Max(Math.Min(gauge.Maximum, gauge.Value), gauge.Minimum) / (gauge.Maximum - gauge.Minimum);
        var animation = new DoubleAnimation(relativeValue, TimeSpan.FromSeconds(.5)) { AccelerationRatio = .33, DecelerationRatio = .33 };
        gauge.BeginAnimation(AnimatedValueProperty, animation);
    }

    private static void OnAnimatedAngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var gauge = (MultiColorGauge)d;
        var (_, hand, handLength, canvas, _) = (GaugeMetaData)gauge.TemplateMetadata;
        var relativeValue = (double)e.NewValue;
        hand.RenderTransform = new RotateTransform((relativeValue - 0.5) * 180, 4, handLength - 5);
        canvas.Children.OfType<Rectangle>().Apply(rect => ColorShape(gauge, rect, relativeValue));
    }

    private static void ColorShape(MultiColorGauge gauge, Shape rect, double relativeValue)
    {
        var rectRelativeValue = Math.Round((double)rect.Tag, 6);

        if (rectRelativeValue > relativeValue || relativeValue <= 0 || gauge.GaugeColors == null)
        {
            rect.Fill = Brushes.LightGray;
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

        var (_, _, _, canvas, _) = (GaugeMetaData)gauge.TemplateMetadata;

        try
        {
            canvas.Children.OfType<Rectangle>().ToList().Apply(canvas.Children.Remove);

            for (double angle = 0; angle < 180.0001; angle += 6.9230769231)
            {
                const double height = 5;
                const double width = 18;
                var (sin, cos) = Math.SinCos(angle / (180 / Math.PI));
                var translationLength = canvas.Width / 2 - width / 2;

                var rect = new Rectangle
                {
                    Height = height,
                    Width = width,
                    Fill = Brushes.LightGray,
                    StrokeThickness = 0,
                    Stroke = Brushes.Transparent,
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
            SetValue(gauge);
        }
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
