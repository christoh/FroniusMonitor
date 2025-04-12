using Avalonia.Controls;
using Avalonia.Media.Immutable;
using De.Hochstaetter.HomeAutomationClient.Extensions;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

public partial class LinearGauge : Gauge
{
    public static readonly StyledProperty<double> GaugeHeightProperty = AvaloniaProperty.Register<LinearGauge, double>(nameof(GaugeHeight), 6d);

    public double GaugeHeight
    {
        get => GetValue(GaugeHeightProperty);
        set => SetValue(GaugeHeightProperty, value);
    }

    public LinearGauge()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        switch (change.Property.Name)
        {
            case nameof(Origin):
            case nameof(Minimum):
            case nameof(Maximum):
            case nameof(Value):
                SetValue();
                break;
            
            case nameof(GaugeColors):
                SetColor();
                break;
            
            case nameof(AnimatedValue):
                OnAnimatedValueChanged();
                break;
        }
    }

    private void OnAnimatedValueChanged()
    {
        var width = Grid.Width * Math.Abs(AnimatedValue - Origin);

        MidpointMarker.IsVisible = Origin > .00001;
        MidpointMarker.Margin = new Thickness(Origin * Grid.Width - MidpointMarker.Width / 2, 0, 0, 0);

        InnerBorder.Width = width;
        InnerBorder.Margin = new Thickness(AnimatedValue > Origin ? Grid.Width * Origin : Grid.Width * Origin - width, 0, 0, 0);
        SetColor();
    }

    private void SetColor()
    {
        var baseColor = GetColorForRelativeValue(AnimatedValue);

        var brush = new ImmutableLinearGradientBrush(
            [
                new ImmutableGradientStop(0,baseColor),
                new ImmutableGradientStop(1,baseColor.MixWith(Colors.Black,.6f)),
            ], startPoint: new RelativePoint(0, 0, RelativeUnit.Relative),endPoint: new RelativePoint(0, 1, RelativeUnit.Relative)
            ); //new SolidColorBrush(baseColor).ToImmutable();   //( baseColor, baseColor * 0.4f + Colors.Black * 0.6f, 90);

        InnerBorder.Background = brush;
    }
}
