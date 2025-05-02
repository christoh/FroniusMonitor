using Avalonia.Media.Immutable;
using System.Text.RegularExpressions;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.HomeAutomationClient.Extensions;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

public partial class EnergyStorageControl : UserControl
{
    #region Avalonia Properties

    public static readonly StyledProperty<Gen24System?> Gen24SystemProperty = AvaloniaProperty.Register<SmartMeterControl, Gen24System?>(nameof(Gen24System));

    public Gen24System? Gen24System
    {
        get => GetValue(Gen24SystemProperty);
        set => SetValue(Gen24SystemProperty, value);
    }

    public static readonly StyledProperty<Color> Soc0Property = AvaloniaProperty.Register<SmartMeterControl, Color>(nameof(Soc0));

    public Color Soc0
    {
        get => GetValue(Soc0Property);
        set => SetValue(Soc0Property, value);
    }

    public static readonly StyledProperty<Color> Soc1Property = AvaloniaProperty.Register<SmartMeterControl, Color>(nameof(Soc1));

    public Color Soc1
    {
        get => GetValue(Soc1Property);
        set => SetValue(Soc1Property, value);
    }

    public static readonly StyledProperty<Color> Soc2Property = AvaloniaProperty.Register<SmartMeterControl, Color>(nameof(Soc2));

    public Color Soc2
    {
        get => GetValue(Soc2Property);
        set => SetValue(Soc2Property, value);
    }

    public static readonly StyledProperty<Color> Soc3Property = AvaloniaProperty.Register<SmartMeterControl, Color>(nameof(Soc3));

    public Color Soc3
    {
        get => GetValue(Soc3Property);
        set => SetValue(Soc3Property, value);
    }

    public static readonly StyledProperty<Color> Soc4Property = AvaloniaProperty.Register<SmartMeterControl, Color>(nameof(Soc4));

    public Color Soc4
    {
        get => GetValue(Soc4Property);
        set => SetValue(Soc4Property, value);
    }

    public static readonly StyledProperty<Color> Soc5Property = AvaloniaProperty.Register<SmartMeterControl, Color>(nameof(Soc5));

    public Color Soc5
    {
        get => GetValue(Soc5Property);
        set => SetValue(Soc5Property, value);
    }

    public static readonly StyledProperty<IBrush?> StatusRedProperty = AvaloniaProperty.Register<SmartMeterControl, IBrush?>(nameof(StatusRed));

    public IBrush? StatusRed
    {
        get => GetValue(StatusRedProperty);
        set => SetValue(StatusRedProperty, value);
    }

    public static readonly StyledProperty<IBrush?> StatusGreenProperty = AvaloniaProperty.Register<SmartMeterControl, IBrush?>(nameof(StatusGreen));

    public IBrush? StatusGreen
    {
        get => GetValue(StatusGreenProperty);
        set => SetValue(StatusGreenProperty, value);
    }

    public static readonly StyledProperty<IBrush?> StatusYellowProperty = AvaloniaProperty.Register<SmartMeterControl, IBrush?>(nameof(StatusYellow));

    public IBrush? StatusYellow
    {
        get => GetValue(StatusYellowProperty);
        set => SetValue(StatusYellowProperty, value);
    }

    #endregion

    public EnergyStorageControl()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        switch (change.Property.Name)
        {
            case nameof(Gen24System):
            case nameof(Soc0):
            case nameof(Soc1):
            case nameof(Soc2):
            case nameof(Soc3):
            case nameof(Soc4):
            case nameof(Soc5):
            case nameof(StatusRed):
            case nameof(StatusGreen):
            case nameof(StatusYellow):
                SetBatteryColor();
                break;
        }
    }

    private void SetBatteryColor()
    {
        SocRectangle.Height = (Gen24System?.Sensors?.Storage?.StateOfCharge ?? 0) * BackgroundRectangle.Height;
        SocRectangle.Fill = new ImmutableSolidColorBrush(ComposeColor());

        Enclosure.BorderBrush = PlusPole.Background = Gen24System?.Sensors?.Storage?.TrafficLight switch
        {
            TrafficLight.Green => StatusGreen,
            TrafficLight.Red => StatusRed,
            _ => StatusYellow,
        };
    }

    private Color ComposeColor()
    {
        IReadOnlyList<ColorThreshold> batteryColors =
        [
            new(0, Soc0),
            new(0.05, Soc1),
            new(0.25, Soc2),
            new(0.40, Soc3),
            new(0.5, Soc4),
            new(1, Soc5),
        ];

        var soc = Gen24System?.Sensors?.Storage?.StateOfCharge ?? 0;
        Color color;

        switch (soc)
        {
            case <= 0:
                color = batteryColors[0].Color;
                break;

            case >= 1:
                color = batteryColors[^1].Color;
                break;

            default:
                var lower = batteryColors.Last(bc => soc >= bc.RelativeValue);
                var upper = batteryColors.First(bc => bc.RelativeValue > lower.RelativeValue);
                var percentage = (float)((soc - lower.RelativeValue) / (upper.RelativeValue - lower.RelativeValue));
                color = lower.Color.MixWith(upper.Color, percentage);
                break;
        }

        return color;
    }
}