using System.Diagnostics;
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


    private void OnInitialized(object? sender, EventArgs e)
    {
        SetValue(true);
        SetColor();
    }
    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        switch (change.Property.Name)
        {
            case nameof(Minimum):
            case nameof(Maximum):
            case nameof(Value):
                SetValue();
                break;

            case nameof(GaugeColors):
                SetColor();
                break;

            case nameof(AnimatedValue):
            case nameof(Origin):
                OnAnimatedValueChanged();
                break;
        }
    }

    protected override void SetValue(bool sKipAnimation = false)
    {
        try
        {
            var value = ShowPercent ? (Value - Minimum) / (Maximum - Minimum) * 100 : Value;
            ValueTextBlock.Text = value.ToString(StringFormat, CultureInfo.CurrentCulture);
        }
        finally
        {
            base.SetValue(sKipAnimation);
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
        if (GaugeColors == null)
        {
            return;
        }
        
        var lowerValue = Math.Min(Origin, AnimatedValue);
        var upperValue = Math.Max(Origin, AnimatedValue);

        var gradientStops = GaugeColors
            .Where(g => g.RelativeValue < upperValue && g.RelativeValue > lowerValue)
            .Select(g => new ImmutableGradientStop((g.RelativeValue - lowerValue) / (upperValue - lowerValue), g.Color))
            .Prepend(new ImmutableGradientStop(0, GetColorForRelativeValue(lowerValue)))
            .Append(new ImmutableGradientStop(1, GetColorForRelativeValue(upperValue)))
            .ToList()
            ;

        var brush = new ImmutableLinearGradientBrush(gradientStops);

        InnerBorder.Background = brush;
    }
}
