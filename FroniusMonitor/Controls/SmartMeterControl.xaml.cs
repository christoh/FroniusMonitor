using System.ComponentModel;
using System.Windows;
using De.Hochstaetter.Fronius.Models;
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
    More
}

public partial class SmartMeterControl
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

    private int currentPowerModeIndex, currentVoltageModeIndex, currentCurrentIndex;

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

    private void SmartMeterDataChanged(object? sender = null, PropertyChangedEventArgs? e = null)
    {
        Dispatcher.InvokeAsync(() =>
        {
            var data = SmartMeter?.Data;

            if (data == null)
            {
                return;
            }

            switch (Mode)
            {
                case MeterDisplayMode.PowerReal:
                    Header.Text = Loc.RealPower;
                    Value1.Text = $"{data.L1RealPower:N1} W";
                    Value2.Text = $"{data.L2RealPower:N1} W";
                    Value3.Text = $"{data.L3RealPower:N1} W";
                    ValueSum.Text = $"{data.TotalRealPower:N1} W";
                    SetL123("Sum");
                    break;

                case MeterDisplayMode.PowerApparent:
                    Header.Text = Loc.ApparentPower;
                    Value1.Text = $"{data.L1ApparentPower:N1} W";
                    Value2.Text = $"{data.L2ApparentPower:N1} W";
                    Value3.Text = $"{data.L3ApparentPower:N1} W";
                    ValueSum.Text = $"{data.TotalApparentPower:N1} W";
                    SetL123("Sum");
                    break;

                case MeterDisplayMode.PowerReactive:
                    Header.Text = Loc.ReactivePower;
                    Value1.Text = $"{data.L1ReactivePower:N1} W";
                    Value2.Text = $"{data.L2ReactivePower:N1} W";
                    Value3.Text = $"{data.L3ReactivePower:N1} W";
                    ValueSum.Text = $"{data.TotalReactivePower:N1} W";
                    SetL123("Sum");
                    break;

                case MeterDisplayMode.PowerFactor:
                    Header.Text = Loc.PowerFactor;
                    Value1.Text = $"{data.L1PowerFactor:N3}";
                    Value2.Text = $"{data.L2PowerFactor:N3}";
                    Value3.Text = $"{data.L3PowerFactor:N3}";
                    ValueSum.Text = $"{data.TotalPowerFactor:N3}";
                    SetL123("Tot");
                    break;

                case MeterDisplayMode.PhaseVoltage:
                    Header.Text = Loc.PhaseVoltage;
                    Value1.Text = $"{data.L1Voltage:N1} V";
                    Value2.Text = $"{data.L2Voltage:N1} V";
                    Value3.Text = $"{data.L3Voltage:N1} V";
                    ValueSum.Text = $"{data.AverageVoltage:N1} V";
                    SetL123("Avg");
                    break;

                case MeterDisplayMode.LineVoltage:
                    Header.Text = Loc.LineVoltage;
                    Value1.Text = $"{data.L1L2Voltage:N1} V";
                    Value2.Text = $"{data.L2L3Voltage:N1} V";
                    Value3.Text = $"{data.L3L1Voltage:N1} V";
                    ValueSum.Text = $"{data.AverageTwoPhasesVoltage:N1} V";
                    SetTwoPhases("Avg");
                    break;

                case MeterDisplayMode.Current:
                    Header.Text = Loc.Current;
                    Value1.Text = $"{data.L1Current:N3} A";
                    Value2.Text = $"{data.L2Current:N3} A";
                    Value3.Text = $"{data.L3Current:N3} A";
                    ValueSum.Text = $"{data.TotalCurrent:N3} A";
                    SetL123("Sum");
                    break;

                case MeterDisplayMode.PowerOutOfBalance:
                    Header.Text = Loc.OutOfBalance;
                    Value1.Text = $"{data.L1L2OutOfBalancePower:N1} W";
                    Value2.Text = $"{data.L2L3OutOfBalancePower:N1} W";
                    Value3.Text = $"{data.L3L1OutOfBalancePower:N1} W";
                    ValueSum.Text = $"{data.MaxOutOfBalancePower:N1} W";
                    SetTwoPhases("Max");
                    break;

                case MeterDisplayMode.More:
                    Header.Text = SmartMeter?.SerialNumber;
                    Value1.Text = $"{data.Frequency:N2} Hz";
                    Label1.Text = "Frq";
                    Value2.Text = $"{data.MeterTimestamp.ToLocalTime():d}";
                    Label2.Text = "Dat";
                    Value3.Text = $"{data.MeterTimestamp.ToLocalTime():T}";
                    Label3.Text = "Tim";
                    ValueSum.Text = $"{data.IsVisible}";
                    LabelSum.Text = "Val";
                    break;

                case MeterDisplayMode.CurrentOutOfBalance:
                    Header.Text = Loc.OutOfBalance;
                    Value1.Text = $"{data.L1L2OutOfBalanceCurrent:N3} A";
                    Value2.Text = $"{data.L2L3OutOfBalanceCurrent:N3} A";
                    Value3.Text = $"{data.L3L1OutOfBalanceCurrent:N3} A";
                    ValueSum.Text = $"{data.MaxOutOfBalanceCurrent:N3} A";
                    SetTwoPhases("Max");
                    break;
            }
        });
    }

    private void SetL123(string sumText)
    {
        Label1.Text = "L1";
        Label2.Text = "L2";
        Label3.Text = "L3";
        LabelSum.Text = sumText;
    }

    private void SetTwoPhases(string sumText)
    {
        Label1.Text = "L12";
        Label2.Text = "L23";
        Label3.Text = "L31";
        LabelSum.Text = sumText;
    }

    private void SetMode(IReadOnlyList<MeterDisplayMode> modeList, ref int index)
    {
        index = modeList.Contains(Mode) ? ++index % modeList.Count : 0;
        Mode = modeList[index];
    }

    private void OnPowerClick(object sender, RoutedEventArgs e) => SetMode(powerModes, ref currentPowerModeIndex);

    private void OnVoltageClick(object sender, RoutedEventArgs e) => SetMode(voltageModes, ref currentVoltageModeIndex);

    private void OnCurrentClick(object sender, RoutedEventArgs e) => SetMode(currentModes, ref currentCurrentIndex);

    private void OnMoreClick(object sender, RoutedEventArgs e) => Mode = MeterDisplayMode.More;
}
