using System.Windows.Shapes;

namespace De.Hochstaetter.FroniusMonitor.Wpf.Resources;

public partial class HalfCircleGauge
{
    private readonly DependencyPropertyDescriptor? valueDescriptor = DependencyPropertyDescriptor.FromProperty(RangeBase.ValueProperty, typeof(MultiColorGauge));
    private readonly DependencyPropertyDescriptor? minimumDescriptor = DependencyPropertyDescriptor.FromProperty(RangeBase.MinimumProperty, typeof(MultiColorGauge));
    private readonly DependencyPropertyDescriptor? maximumDescriptor = DependencyPropertyDescriptor.FromProperty(RangeBase.MaximumProperty, typeof(MultiColorGauge));
    private readonly DependencyPropertyDescriptor? colorsDescriptor = DependencyPropertyDescriptor.FromProperty(MultiColorGauge.GaugeColorsProperty, typeof(MultiColorGauge));

    public HalfCircleGauge()
    {
        InitializeComponent();
    }

    private void OnTemplateLoaded(object sender, EventArgs? _ = null)
    {
        if (sender is not FrameworkElement { TemplatedParent: MultiColorGauge gauge })
        {
            return;
        }

        valueDescriptor?.AddValueChanged(gauge, OnValueChanged);
        minimumDescriptor?.AddValueChanged(gauge, OnValueChanged);
        maximumDescriptor?.AddValueChanged(gauge, OnValueChanged);
        colorsDescriptor?.AddValueChanged(gauge, OnValueChanged);
        OnParametersChanged(gauge);
    }

    private static void OnValueChanged(object? sender, EventArgs? _ = null)
    {
        var (gauge, canvas) = GetGaugeAndCanvas(sender);

        if (canvas == null)
        {
            return;
        }

        SetValue(gauge!, canvas);
    }

    private static void SetValue(MultiColorGauge gauge, Canvas canvas)
    {
        var relativeValue = Math.Max(Math.Min(gauge.Maximum, gauge.Value), gauge.Minimum) / (gauge.Maximum - gauge.Minimum);
        canvas.Children.OfType<Rectangle>().Apply(rect => ColorShape(rect, gauge, relativeValue));
    }

    private static void ColorShape(Shape rect, MultiColorGauge gauge, double relativeValue)
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
        var (gauge, canvas) = GetGaugeAndCanvas(sender);

        if (canvas == null)
        {
            return;
        }

        try
        {
            canvas.Children.OfType<Rectangle>().ToList().Apply(canvas.Children.Remove);

            for (double angle = 0; angle < 180.0001; angle += 6.9230769231)
            {
                const double height = 5;
                const double width = 18;
                var (sin, cos) = Math.SinCos(angle / (180 / Math.PI));
                var translationLength = width / 2 - canvas.Height;
                var transform = new TransformGroup();
                transform.Children.Add(new RotateTransform(angle, width / 2, height / 2));
                transform.Children.Add(new TranslateTransform(cos * translationLength, sin * translationLength));

                var rect = new Rectangle
                {
                    Height = height,
                    Width = width,
                    Fill = Brushes.LightGray,
                    StrokeThickness = 0,
                    Stroke = Brushes.Transparent,
                    RenderTransform = transform,
                    Tag = angle / 180,
                };

                rect.SetValue(Canvas.BottomProperty, 0d);
                rect.SetValue(Canvas.LeftProperty, (canvas.Width - width) / 2);
                canvas.Children.Add(rect);
            }
        }
        finally
        {
            SetValue(gauge!, canvas);
        }
    }

    private static (MultiColorGauge? Gauge, Canvas? Canvas) GetGaugeAndCanvas(object? sender)
    {
        if (sender is MultiColorGauge gauge && gauge.Template.FindName("Canvas", gauge) is Canvas canvas)
        {
            return (gauge, canvas);
        }

        return (null, null);
    }
}
