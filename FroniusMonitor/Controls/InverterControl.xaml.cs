using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.FroniusMonitor.Contracts;
using Loc = De.Hochstaetter.Fronius.Localization.Resources;

namespace De.Hochstaetter.FroniusMonitor.Controls;

public enum InverterDisplayMode
{
    AcVoltage,
    AcCurrent,
    AcPower,
    DcVoltage,
    DcCurrent,
    DcPower,
    Energy,
    More,
}

public partial class InverterControl : IHaveLcdPanel
{
    private static readonly IReadOnlyList<InverterDisplayMode> acModes = new[] { InverterDisplayMode.AcPower, InverterDisplayMode.AcCurrent, InverterDisplayMode.AcVoltage };
    private static readonly IReadOnlyList<InverterDisplayMode> dcModes = new[] { InverterDisplayMode.DcPower, InverterDisplayMode.DcCurrent, InverterDisplayMode.DcVoltage };
    private int currentAcIndex, currentDcIndex;


    #region Dependency Properties

    public static readonly DependencyProperty InverterProperty = DependencyProperty.Register
    (
        nameof(Inverter), typeof(Inverter), typeof(InverterControl),
        new PropertyMetadata((d, e) => ((InverterControl)d).OnInverterChanged(e))
    );

    public Inverter? Inverter
    {
        get => (Inverter?)GetValue(InverterProperty);
        set => SetValue(InverterProperty, value);
    }

    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register
    (
        nameof(Mode), typeof(InverterDisplayMode), typeof(InverterControl),
        new PropertyMetadata(InverterDisplayMode.AcPower, (d, _) => ((InverterControl)d).OnModeChanged())
    );

    public InverterDisplayMode Mode
    {
        get => (InverterDisplayMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    #endregion

    public InverterControl()
    {
        InitializeComponent();

        Unloaded += (_, _) =>
        {
            if (Inverter != null)
            {
                Inverter.PropertyChanged -= OnInverterDataChanged;
            }
        };
    }

    private void OnModeChanged() => OnInverterDataChanged();

    private void OnInverterChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is Inverter oldInverter)
        {
            oldInverter.PropertyChanged -= OnInverterDataChanged;
        }

        if (Inverter != null)
        {
            Inverter.PropertyChanged += OnInverterDataChanged;
        }


    }

    private void OnInverterDataChanged(object? _ = null, PropertyChangedEventArgs? e = null)
    {
        if (e != null && e.PropertyName != nameof(Inverter.Data))
        {
            return;
        }

        Dispatcher.InvokeAsync(() =>
        {
            BackgroundProvider.Background = Inverter?.Data?.Status switch
            {
                InverterStatus.Running => Brushes.AntiqueWhite,
                InverterStatus.Error => Brushes.Red,
                _ => Brushes.LightGray,
            };

            switch (Mode)
            {
                case InverterDisplayMode.AcVoltage:
                    SetL123("Avg");
                    Lcd.Header = Loc.AcVoltage;

                    SetLcdValues
                    (
                        Inverter?.ThreePhasesData?.L1Voltage,
                        Inverter?.ThreePhasesData?.L2Voltage,
                        Inverter?.ThreePhasesData?.L3Voltage,
                        Inverter?.Data?.TotalVoltage,
                        "N1", "V"
                    );

                    break;

                case InverterDisplayMode.AcCurrent:
                    SetL123("Sum");
                    Lcd.Header = Loc.AcCurrent;

                    SetLcdValues
                    (
                        Inverter?.ThreePhasesData?.L1Current,
                        Inverter?.ThreePhasesData?.L2Current,
                        Inverter?.ThreePhasesData?.L3Current,
                        Inverter?.Data?.TotalCurrent,
                        "N3", "A"
                    );

                    break;

                case InverterDisplayMode.AcPower:
                    SetL123("Sum");
                    Lcd.Header = Loc.AcPower;

                    SetLcdValues
                    (
                        Inverter?.ThreePhasesData?.L1PowerWatts,
                        Inverter?.ThreePhasesData?.L2PowerWatts,
                        Inverter?.ThreePhasesData?.L3PowerWatts,
                        Inverter?.Data?.AcPowerWatts,
                        "N1", "W"
                    );

                    break;

                case InverterDisplayMode.DcVoltage:
                    SetDc123(string.Empty);
                    Lcd.Header = Loc.DcVoltage;

                    SetLcdValues
                    (
                        Inverter?.Data?.VoltageString1,
                        Inverter?.Data?.VoltageString2,
                        Inverter?.Data?.VoltageString3,
                        null,
                        "N1", "V", string.Empty
                    );

                    break;

                case InverterDisplayMode.DcCurrent:
                    SetDc123(string.Empty);
                    Lcd.Header = Loc.DcCurrent;

                    SetLcdValues
                    (
                        Inverter?.Data?.CurrentString1,
                        Inverter?.Data?.CurrentString2,
                        Inverter?.Data?.CurrentString3,
                        null,
                        "N3", "A", string.Empty
                    );

                    break;

                case InverterDisplayMode.DcPower:
                    SetDc123("Sum");
                    Lcd.Header = Loc.DcPower;

                    SetLcdValues
                    (
                        Inverter?.Data?.String1PowerWatts,
                        Inverter?.Data?.String2PowerWatts,
                        Inverter?.Data?.String3PowerWatts,
                        Inverter?.Data?.SolarPowerWatts,
                        "N1", "W"
                    );

                    break;

                case InverterDisplayMode.Energy:
                    Lcd.Label1 = "Day";
                    Lcd.Label2 = "Yr";
                    Lcd.Label3 = "Tot";
                    Lcd.Value1 = ToLcd(Inverter?.Data?.DayEnergyKiloWattHours, "N3", "kWh");
                    Lcd.Value2 = ToLcd(Inverter?.Data?.YearEnergyMegaWattHours, "N3", "MWh");
                    Lcd.Value3 = ToLcd(Inverter?.Data?.TotalEnergyMegaWattHours, "N3", "MWh");
                    Lcd.LabelSum = Lcd.ValueSum = string.Empty;
                    break;

                case InverterDisplayMode.More:
                    Lcd.Header = Inverter?.SerialNumber;
                    Lcd.Value1 = ToLcd(Inverter?.Data?.Frequency,"N2","Hz");
                    Lcd.Label1 = "Frq";
                    Lcd.Value2 = $"{Inverter?.Data?.Timestamp.ToLocalTime():d}";
                    Lcd.Label2 = "Dat";
                    Lcd.Value3 = $"{Inverter?.Data?.Timestamp.ToLocalTime():T}";
                    Lcd.Label3 = "Tim";
                    Lcd.LabelSum = Lcd.ValueSum = string.Empty;
                    break;
            }
        });
    }

    private void SetLcdValues(double? value1, double? value2, double? value3, double? aggregatedValue, string format, string unit, string nullValue = "---")
    {
        Lcd.Value1 = ToLcd(value1, format, unit);
        Lcd.Value2 = ToLcd(value2, format, unit);
        Lcd.Value3 = ToLcd(value3, format, unit);
        Lcd.ValueSum = ToLcd(aggregatedValue, format, unit, nullValue);
    }

    private string ToLcd(double? value, string format, string unit, string nullValue = "---")
    {
        return value.HasValue ? value.Value.ToString(format, CultureInfo.CurrentCulture) + ' ' + unit : nullValue;
    }

    private void SetL123(string sumText) => IHaveLcdPanel.SetL123(Lcd, sumText);

    private void SetDc123(string sumText)
    {
        Lcd.Label1 = "DC1";
        Lcd.Label2 = "DC2";
        Lcd.Label3 = "DC3";
        Lcd.LabelSum = sumText;
    }

    private void SetMode(IReadOnlyList<InverterDisplayMode> modeList, ref int index)
    {
        index = modeList.Contains(Mode) ? ++index % modeList.Count : 0;
        Mode = modeList[index];
    }

    private void OnAcClicked(object sender, RoutedEventArgs e) => SetMode(acModes, ref currentAcIndex);

    private void OnDcClicked(object sender, RoutedEventArgs e) => SetMode(dcModes, ref currentDcIndex);

    private void OnEnergyClicked(object sender, RoutedEventArgs e) => Mode = InverterDisplayMode.Energy;

    private void OnMoreClicked(object sender, RoutedEventArgs e) => Mode = InverterDisplayMode.More;
}
