using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.FroniusMonitor.Contracts;
using Loc = De.Hochstaetter.Fronius.Localization.Resources;

namespace De.Hochstaetter.FroniusMonitor.Controls;

public enum MeterDisplayMode
{
    PowerReal,
    PowerApparent,
    PowerReactive,
    PowerFactor,
    PowerOutOfBalance,
    PhaseVoltage,
    LineVoltage,
    Current,
    CurrentOutOfBalance,
    More,
    MoreEnergy,
}

public partial class SmartMeterControl : IHaveLcdPanel
{
    private static readonly IReadOnlyList<MeterDisplayMode> powerModes = new[]
    {
        MeterDisplayMode.PowerReal, MeterDisplayMode.PowerApparent,
        MeterDisplayMode.PowerReactive, MeterDisplayMode.PowerFactor,
        MeterDisplayMode.PowerOutOfBalance
    };

    private static readonly IReadOnlyList<MeterDisplayMode> voltageModes = new[]
    {
        MeterDisplayMode.PhaseVoltage, MeterDisplayMode.LineVoltage
    };

    private static readonly IReadOnlyList<MeterDisplayMode> currentModes = new[]
    {
        MeterDisplayMode.Current, MeterDisplayMode.CurrentOutOfBalance
    };

    private static readonly IReadOnlyList<MeterDisplayMode> moreModes = new[]
    {
        MeterDisplayMode.MoreEnergy, MeterDisplayMode.More
    };

    private int currentPowerModeIndex, currentVoltageModeIndex, currentCurrentIndex, currentMoreIndex;

    #region Dependency Properties

    public static readonly DependencyProperty SmartMeterProperty = DependencyProperty.Register
    (
        nameof(SmartMeter), typeof(SmartMeter), typeof(SmartMeterControl),
        new PropertyMetadata((d, e) => ((SmartMeterControl)d).OnSmartMeterChanged(e))
    );

    public SmartMeter? SmartMeter
    {
        get => (SmartMeter?)GetValue(SmartMeterProperty);
        set => SetValue(SmartMeterProperty, value);
    }

    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register
    (
        nameof(Mode), typeof(MeterDisplayMode), typeof(SmartMeterControl),
        new PropertyMetadata(MeterDisplayMode.PowerReal, (d, _) => ((SmartMeterControl)d).OnModeChanged())
    );

    public MeterDisplayMode Mode
    {
        get => (MeterDisplayMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    #endregion

    public SmartMeterControl()
    {
        InitializeComponent();

        Unloaded += (_, _) =>
        {
            if (SmartMeter != null)
            {
                SmartMeter.PropertyChanged -= SmartMeterDataChanged;
            }
        };
    }

    private void OnSmartMeterChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is SmartMeter oldSmartMeter)
        {
            oldSmartMeter.PropertyChanged -= SmartMeterDataChanged;
        }

        if (e.NewValue is SmartMeter newSmartMeter)
        {
            newSmartMeter.PropertyChanged += SmartMeterDataChanged;
        }

        SmartMeterDataChanged();
    }

    private void OnModeChanged() => SmartMeterDataChanged();

    private void SmartMeterDataChanged(object? sender = null, PropertyChangedEventArgs? e = null) => Dispatcher.InvokeAsync(() =>
    {
        var data = SmartMeter?.Data;

        if (data is null)
        {
            BackgroundProvider.Background = Brushes.LightGray;
            return;
        }

        BackgroundProvider.Background = !data.IsVisible ? Brushes.Red : data.IsEnabled ? Brushes.AntiqueWhite : Brushes.LightGray;

        switch (Mode)
        {
            case MeterDisplayMode.PowerReal:
                Lcd.Header = Loc.RealPower;
                Lcd.Value1 = $"{data.L1RealPower:N1} W";
                Lcd.Value2 = $"{data.L2RealPower:N1} W";
                Lcd.Value3 = $"{data.L3RealPower:N1} W";
                Lcd.ValueSum = $"{data.TotalRealPower:N1} W";
                SetL123("Sum");
                break;

            case MeterDisplayMode.PowerApparent:
                Lcd.Header = Loc.ApparentPower;
                Lcd.Value1 = $"{data.L1ApparentPower:N1} W";
                Lcd.Value2 = $"{data.L2ApparentPower:N1} W";
                Lcd.Value3 = $"{data.L3ApparentPower:N1} W";
                Lcd.ValueSum = $"{data.TotalApparentPower:N1} W";
                SetL123("Sum");
                break;

            case MeterDisplayMode.PowerReactive:
                Lcd.Header = Loc.ReactivePower;
                Lcd.Value1 = $"{data.L1ReactivePower:N1} W";
                Lcd.Value2 = $"{data.L2ReactivePower:N1} W";
                Lcd.Value3 = $"{data.L3ReactivePower:N1} W";
                Lcd.ValueSum = $"{data.TotalReactivePower:N1} W";
                SetL123("Sum");
                break;

            case MeterDisplayMode.PowerFactor:
                Lcd.Header = Loc.PowerFactor;
                Lcd.Value1 = $"{data.L1PowerFactor:N3}";
                Lcd.Value2 = $"{data.L2PowerFactor:N3}";
                Lcd.Value3 = $"{data.L3PowerFactor:N3}";
                Lcd.ValueSum = $"{data.TotalPowerFactor:N3}";
                SetL123("Tot");
                break;

            case MeterDisplayMode.PhaseVoltage:
                Lcd.Header = Loc.PhaseVoltage;
                Lcd.Value1 = $"{data.L1Voltage:N1} V";
                Lcd.Value2 = $"{data.L2Voltage:N1} V";
                Lcd.Value3 = $"{data.L3Voltage:N1} V";
                Lcd.ValueSum = $"{data.AverageVoltage:N1} V";
                SetL123("Avg");
                break;

            case MeterDisplayMode.LineVoltage:
                Lcd.Header = Loc.LineVoltage;
                Lcd.Value1 = $"{data.L1L2Voltage:N1} V";
                Lcd.Value2 = $"{data.L2L3Voltage:N1} V";
                Lcd.Value3 = $"{data.L3L1Voltage:N1} V";
                Lcd.ValueSum = $"{data.AverageTwoPhasesVoltage:N1} V";
                SetTwoPhases("Avg");
                break;

            case MeterDisplayMode.Current:
                Lcd.Header = Loc.Current;
                Lcd.Value1 = $"{data.L1Current:N3} A";
                Lcd.Value2 = $"{data.L2Current:N3} A";
                Lcd.Value3 = $"{data.L3Current:N3} A";
                Lcd.ValueSum = $"{data.TotalCurrent:N3} A";
                SetL123("Sum");
                break;

            case MeterDisplayMode.PowerOutOfBalance:
                Lcd.Header = Loc.OutOfBalance;
                Lcd.Value1 = $"{data.L1L2OutOfBalancePower:N1} W";
                Lcd.Value2 = $"{data.L2L3OutOfBalancePower:N1} W";
                Lcd.Value3 = $"{data.L3L1OutOfBalancePower:N1} W";
                Lcd.ValueSum = $"{data.MaxOutOfBalancePower:N1} W";
                SetTwoPhases("Max");
                break;

            case MeterDisplayMode.More:
                Lcd.Header = SmartMeter?.SerialNumber;
                Lcd.Value1 = $"{data.Frequency:N2} Hz";
                Lcd.Label1 = "Frq";
                Lcd.Value2 = $"{data.MeterTimestamp.ToLocalTime():d}";
                Lcd.Label2 = "Dat";
                Lcd.Value3 = $"{data.MeterTimestamp.ToLocalTime():T}";
                Lcd.Label3 = "Tim";
                Lcd.ValueSum = $"{data.IsVisible}";
                Lcd.LabelSum = "Val";
                break;

            case MeterDisplayMode.MoreEnergy:
                Lcd.Header = $"{Loc.Energy} (kWh)";
                Lcd.Value1 = $"{data.RealEnergyConsumedKiloWattHours:N1}";
                Lcd.Label1 = "CRl";
                Lcd.Value2 = $"{data.RealEnergyProducedKiloWattHours:N1}";
                Lcd.Label2 = "PRl";
                Lcd.Value3 = $"{data.ReactiveEnergyConsumedKiloWattHours:N1}";
                Lcd.Label3 = "CRv";
                Lcd.ValueSum = $"{data.ReactiveEnergyProducedKiloWattHours:N1}";
                Lcd.LabelSum = "PRv";
                break;

            case MeterDisplayMode.CurrentOutOfBalance:
                Lcd.Header = Loc.OutOfBalance;
                Lcd.Value1 = $"{data.L1L2OutOfBalanceCurrent:N3} A";
                Lcd.Value2 = $"{data.L2L3OutOfBalanceCurrent:N3} A";
                Lcd.Value3 = $"{data.L3L1OutOfBalanceCurrent:N3} A";
                Lcd.ValueSum = $"{data.MaxOutOfBalanceCurrent:N3} A";
                SetTwoPhases("Max");
                break;
        }
    });

    private void SetL123(string sumText) => IHaveLcdPanel.SetL123(Lcd, sumText);
    private void SetTwoPhases(string sumText) => IHaveLcdPanel.SetTwoPhases(Lcd, sumText);

    private void SetMode(IReadOnlyList<MeterDisplayMode> modeList, ref int index)
    {
        index = modeList.Contains(Mode) ? ++index % modeList.Count : 0;
        Mode = modeList[index];
    }

    private void OnPowerClick(object sender, RoutedEventArgs e) => SetMode(powerModes, ref currentPowerModeIndex);

    private void OnVoltageClick(object sender, RoutedEventArgs e) => SetMode(voltageModes, ref currentVoltageModeIndex);

    private void OnCurrentClick(object sender, RoutedEventArgs e) => SetMode(currentModes, ref currentCurrentIndex);

    private void OnMoreClick(object sender, RoutedEventArgs e) => SetMode(moreModes, ref currentMoreIndex);
}
