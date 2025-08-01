using Avalonia.Media.Immutable;
using De.Hochstaetter.Fronius.Extensions;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

public partial class LinearRectangleGauge : Gauge
{
    public static readonly StyledProperty<int> NumberOfTicksProperty = AvaloniaProperty.Register<LinearRectangleGauge, int>(nameof(NumberOfTicks), 20);

    public int NumberOfTicks
    {
        get => GetValue(NumberOfTicksProperty);
        set => SetValue(NumberOfTicksProperty, value);
    }

    public static readonly StyledProperty<double> RelativeTickWidthProperty = AvaloniaProperty.Register<LinearRectangleGauge, double>(nameof(RelativeTickWidth), 0.8);

    public double RelativeTickWidth
    {
        get => GetValue(RelativeTickWidthProperty);
        set => SetValue(RelativeTickWidthProperty, value);
    }

    public LinearRectangleGauge()
    {
        InitializeComponent();
        CreateTicks();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        switch (change.Property.Name)
        {
            case nameof(Maximum):
            case nameof(Minimum):
            case nameof(Value):
                SetValue();
                break;

            case nameof(NumberOfTicks):
            case nameof(RelativeTickWidth):
                CreateTicks();
                ColorShapes();
                break;

            case nameof(Origin):
            case nameof(TickFill):
            case nameof(AnimatedValue):
                ColorShapes();
                break;

            case nameof(GaugeColors):
                ColorShapes(true);
                break;
        }
    }

    private void CreateTicks()
    {
        RectangleHost.Children.Clear();
        var width = RectangleHost.Width / NumberOfTicks;

        for (var i = 0; i < NumberOfTicks; i++)
        {
            var relativeValue = (double)i / (NumberOfTicks - 1);

            var rect = new Rectangle
            {
                Width = width * RelativeTickWidth,
                Height = 100,
                Fill = TickFill,
                StrokeThickness = 0,
                RenderTransform = new TranslateTransform(i * width, 0),
                Tag = new AngleBrush(relativeValue, new ImmutableSolidColorBrush(GetColorForRelativeValue(relativeValue))),
            };

            RectangleHost.Children.Add(rect);
        }
    }

    private void ColorShapes(bool recalculateColors = false)
    {
        RectangleHost.Children.OfType<Rectangle>().Apply(rect =>
        {
            var (rectRelativeValue, brush) = (AngleBrush)rect.Tag!;

            if (recalculateColors)
            {
                brush = new ImmutableSolidColorBrush(GetColorForRelativeValue(rectRelativeValue));
                rect.Tag = new AngleBrush(rectRelativeValue, brush);
            }

            rectRelativeValue = Math.Round(rectRelativeValue, 8);
            var correctedAnimatedValue = (AnimatedValue - 0.5) * NumberOfTicks / (NumberOfTicks - 1) + 0.5;

            if
            (
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