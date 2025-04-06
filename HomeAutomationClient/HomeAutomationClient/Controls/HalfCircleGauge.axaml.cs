using De.Hochstaetter.Fronius.Extensions;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

public partial class HalfCircleGauge : Gauge
{
    private Polygon? hand;

    public HalfCircleGauge()
    {
        InitializeComponent();
    }

    private void OnTemplateLoaded(object? sender, EventArgs e)
    {
        var handLength = OuterCanvas.Width * 0.45;

        hand = new Polygon
        {
            Points =
            [
                new Point(4, 0), new Point(0, handLength), new Point(8, handLength)
            ],

            Fill = HandFill,
            StrokeThickness = 0,
            RenderTransform = GetHandTransForm(-90),
        };

        hand.SetValue(Canvas.BottomProperty, 8d);
        hand.SetValue(Canvas.LeftProperty, OuterCanvas.Width / 2 - 4);
        OuterCanvas.Children.Add(hand);
        OnParametersChanged();

        if (!ColorAllTicks)
        {
            SetValue(true);
            OnAnimatedAngleChanged();
        }

    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        switch (change.Property.Name)
        {
            case nameof(Maximum):
                MaximumTextBlock.Text = Maximum.ToString(StringFormat);
                SetValue();
                break;

            case nameof(Minimum):
                SetValue();
                MinimumTextBlock.Text = Minimum.ToString(StringFormat);
                break;

            case nameof(Value):
                SetValue();
                break;

            case nameof(AnimatedValue):
                OnAnimatedAngleChanged();
                break;
        }
    }

    private RotateTransform GetHandTransForm(double angleDegrees)
    {
        return new RotateTransform(angleDegrees, 0, OuterCanvas.Width * 0.45 / 2 - 5);
    }

    private void OnAnimatedAngleChanged()
    {
        if (hand == null)
        {
            return;
        }

        hand.RenderTransform = GetHandTransForm((AnimatedValue - 0.5) * 180);

        if (!ColorAllTicks)
        {
            Canvas.Children.OfType<Rectangle>().Apply(ColorShape);
        }
    }

    private void ColorShape(Shape rect)
    {
        var rectRelativeValue = Math.Round((double)rect.Tag!,8);
        var correctedAnimatedValue = (AnimatedValue - 0.5) * 1.02 + 0.5;

        if
        (
            ColorAllTicks ||
            correctedAnimatedValue <= rectRelativeValue && rectRelativeValue <= Origin && (correctedAnimatedValue > 0 || Origin > 0) ||
            correctedAnimatedValue >= rectRelativeValue && rectRelativeValue >= Origin
        )
        {
            IBrush brush = new SolidColorBrush(GetColorForRelativeValue(rectRelativeValue)).ToImmutable();
            rect.Fill = brush;
            return;
        }

        rect.Fill = TickFill;
    }
    private void OnParametersChanged()
    {
        try
        {
            Canvas.Children.OfType<Rectangle>().ToList().Apply(c => Canvas.Children.Remove(c));
            const double height = 5;
            const double width = 18;

            for (double angle = 0; angle < 180.0001; angle += 180d / 26d)
            {
                var (sin, cos) = Math.SinCos(angle / (180 / Math.PI));
                var translationLength = Canvas.Width / 2 - width / 2;

                var rect = new Rectangle
                {
                    Height = height,
                    Width = width,
                    Fill = TickFill,
                    StrokeThickness = 0,
                    RenderTransform = new RotateTransform(angle, 0, 0),
                    Tag = angle / 180,
                };

                rect.SetValue(Canvas.BottomProperty, sin * translationLength);
                rect.SetValue(Canvas.RightProperty, cos * translationLength + Canvas.Width / 2 - width / 2);
                Canvas.Children.Add(rect);
            }
        }
        finally
        {
            if (ColorAllTicks)
            {
                Canvas.Children.OfType<Rectangle>().Apply(ColorShape);
            }
        }
    }
}

public class DemoGaugeExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new HalfCircleGauge
        {
            //Style = new StaticResourceExtension("DefaultHalfCircleGauge").ProvideValue(serviceProvider) as Style ?? throw new NullReferenceException("Style 'DefaultHalfCircleGauge' does not exist"),
            Minimum = 0,
            Maximum = 100,
            Value = 33,
            ColorAllTicks = true,
            GaugeColors = Gauge.HighIsBad,
        };
    }
}
