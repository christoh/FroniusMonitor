using Avalonia.Media.Immutable;
using De.Hochstaetter.Fronius.Extensions;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

public partial class HalfCircleGauge : Gauge
{
    private record AngleBrush(double RelativeValue, IImmutableBrush Brush);
    private readonly Polygon? hand;

    public static readonly StyledProperty<IBrush?> HandFillProperty = AvaloniaProperty.Register<Gauge, IBrush?>(nameof(HandFill), Brushes.DarkSlateGray);

    public IBrush? HandFill
    {
        get => GetValue(HandFillProperty);
        set => SetValue(HandFillProperty, value);
    }

    public static readonly StyledProperty<bool> ColorAllTicksProperty = AvaloniaProperty.Register<Gauge, bool>(nameof(ColorAllTicks));

    public bool ColorAllTicks
    {
        get => GetValue(ColorAllTicksProperty);
        set => SetValue(ColorAllTicksProperty, value);
    }
    
    public HalfCircleGauge()
    {
        InitializeComponent();
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

        try
        {
            MinimumTextBlock.Text = Minimum.ToString(StringFormat);
            MaximumTextBlock.Text = Maximum.ToString(StringFormat);
            Canvas.Children.OfType<Rectangle>().ToList().Apply(c => Canvas.Children.Remove(c));
            const double height = 5;
            const double width = 18;

            for (var angle = 0d; angle < 180.0001; angle += 180d / 26d)
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
                    Tag = new AngleBrush(angle / 180, new ImmutableSolidColorBrush(GetColorForRelativeValue(angle / 180)))
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
                ColorShapes();
            }
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
                MinimumTextBlock.Text = Minimum.ToString(StringFormat);
                SetValue();
                break;
            
            case nameof(StringFormat):
                MaximumTextBlock.Text = Maximum.ToString(StringFormat);
                MinimumTextBlock.Text = Minimum.ToString(StringFormat);
                break;

            case nameof(Value):
                SetValue();
                break;
            
            case nameof(HandFill):
                if (hand == null)
                {
                    return;
                }
                
                hand.Fill = HandFill;
                break;

            case nameof(AnimatedValue):
                OnAnimatedAngleChanged();
                break;
            
            case nameof(ColorAllTicks):
            case nameof(Origin):
            case nameof(TickFill):
                ColorShapes();
                break;

            case nameof(GaugeColors):
                ColorShapes(true);
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
            ColorShapes();
        }
    }

    private void ColorShapes(bool recalculateColors = false)
    {
        Canvas.Children.OfType<Rectangle>().Apply(rect =>
        {
            var (rectRelativeValue, brush) = (AngleBrush)rect.Tag!;

            if (recalculateColors)
            {
                brush = new ImmutableSolidColorBrush(GetColorForRelativeValue(rectRelativeValue));
                rect.Tag = new AngleBrush(rectRelativeValue, brush);
            }

            rectRelativeValue = Math.Round(rectRelativeValue, 8);
            var correctedAnimatedValue = (AnimatedValue - 0.5) * 1.02 + 0.5;

            if
            (
                ColorAllTicks ||
                correctedAnimatedValue <= rectRelativeValue && rectRelativeValue <= Origin && (correctedAnimatedValue > 0 || Origin > 0) ||
                correctedAnimatedValue >= rectRelativeValue && rectRelativeValue >= Origin
            )
            {
                rect.Fill = brush;
                return;
            }

            rect.Fill = TickFill;
        });
    }
}
