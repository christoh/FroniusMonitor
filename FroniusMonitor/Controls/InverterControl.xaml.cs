using Loc = De.Hochstaetter.Fronius.Localization.Resources;

namespace De.Hochstaetter.FroniusMonitor.Controls;

public enum InverterDisplayMode
{
    AcPhaseVoltage,
    AcLineVoltage,
    AcCurrent,
    AcPowerReal,
    AcPowerApparent,
    AcPowerReactive,
    AcPowerFactor,
    DcVoltage,
    DcCurrent,
    DcPower,
    EnergyInverter,
    EnergyStorage,
    EnergySolar,
    More,
    MoreEfficiency,
    MoreTemperatures,
    MoreFans,
    MoreVersions,
    MoreOp,
}

public partial class InverterControl : IHaveLcdPanel
{
    private readonly ISolarSystemService? solarSystemService = IoC.TryGetRegistered<ISolarSystemService>();
    private static readonly IReadOnlyList<InverterDisplayMode> acModes = new[] { InverterDisplayMode.AcPowerReal, InverterDisplayMode.AcPowerApparent, InverterDisplayMode.AcPowerReactive, InverterDisplayMode.AcPowerFactor, InverterDisplayMode.AcCurrent, InverterDisplayMode.AcPhaseVoltage, InverterDisplayMode.AcLineVoltage, };
    private static readonly IReadOnlyList<InverterDisplayMode> dcModes = new[] { InverterDisplayMode.DcPower, InverterDisplayMode.DcCurrent, InverterDisplayMode.DcVoltage, };
    private static readonly IReadOnlyList<InverterDisplayMode> moreModes = new[] { InverterDisplayMode.MoreEfficiency, InverterDisplayMode.More, InverterDisplayMode.MoreTemperatures, InverterDisplayMode.MoreFans, InverterDisplayMode.MoreOp, InverterDisplayMode.MoreVersions };
    private static readonly IReadOnlyList<InverterDisplayMode> energyModes = new[] { InverterDisplayMode.EnergySolar, InverterDisplayMode.EnergyInverter, InverterDisplayMode.EnergyStorage, };
    private int currentAcIndex, currentDcIndex, currentMoreIndex, energyIndex;

    #region Dependency Properties

    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register
    (
        nameof(Mode), typeof(InverterDisplayMode), typeof(InverterControl),
        new PropertyMetadata(InverterDisplayMode.AcPowerReal, (d, _) => ((InverterControl)d).OnModeChanged())
    );

    public InverterDisplayMode Mode
    {
        get => (InverterDisplayMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public static readonly DependencyProperty IsSecondaryProperty = DependencyProperty.Register
    (
        nameof(IsSecondary), typeof(bool), typeof(InverterControl)
    );

    public bool IsSecondary
    {
        get => (bool)GetValue(IsSecondaryProperty);
        set => SetValue(IsSecondaryProperty, value);
    }

    public static readonly DependencyProperty VersionsProperty = DependencyProperty.Register
    (
        nameof(Versions), typeof(Gen24Versions), typeof(InverterControl)
    );

    public Gen24Versions? Versions
    {
        get => (Gen24Versions?)GetValue(VersionsProperty);
        set => SetValue(VersionsProperty, value);
    }

    #endregion

    public InverterControl()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            if (solarSystemService != null)
            {
                solarSystemService.NewDataReceived += NewDataReceived;
            }
        };

        Unloaded += (_, _) =>
        {
            if (solarSystemService != null)
            {
                solarSystemService.NewDataReceived -= NewDataReceived;
            }
        };
    }

    private void OnModeChanged() => NewDataReceived(this, new SolarDataEventArgs(solarSystemService?.SolarSystem));

    private void NewDataReceived(object? sender, SolarDataEventArgs e)
    {

        Dispatcher.InvokeAsync(() =>
        {
            var gen24 = IsSecondary ? e.SolarSystem?.Gen24System2 : e.SolarSystem?.Gen24System;
            var cache = gen24?.Cache;
            var inverter = gen24?.Inverter;
            var dataManager = gen24?.DataManager;
            var powerFlow = e.SolarSystem?.SitePowerFlow;
            var gen24Common = IsSecondary ? e.SolarSystem?.Gen24Common2 : e.SolarSystem?.Gen24Common;

            if (cache == null && inverter == null && dataManager == null)
            {
                return;
            }

            BackgroundProvider.Background = gen24?.InverterStatus?.ToBrush() ?? Brushes.LightGray;
            InverterModelName.Text = $"{gen24?.Inverter?.Model ?? "---"} ({gen24?.InverterStatus?.StatusMessage ?? Loc.Unknown})";
            InverterName.Text = gen24Common?.DisplayName ?? "---";
            VersionList.Visibility = Visibility.Collapsed;
            Lcd.Visibility = Visibility.Visible;

            switch (Mode)
            {
                case InverterDisplayMode.AcPhaseVoltage:
                    SetL123("Avg");
                    Lcd.Header = Loc.PhaseVoltage;

                    SetLcdValues
                    (
                        cache?.InverterPhaseVoltageL1 ?? inverter?.AcVoltageL1,
                        cache?.InverterPhaseVoltageL2 ?? inverter?.AcVoltageL2,
                        cache?.InverterPhaseVoltageL3 ?? inverter?.AcVoltageL3,
                        cache?.InverterPhaseVoltageAverage ?? inverter?.AcPhaseVoltageAverage,
                        "N1", "V"
                    );

                    break;

                case InverterDisplayMode.AcLineVoltage:
                    SetTwoPhases("Avg");
                    Lcd.Header = Loc.LineVoltage;

                    SetLcdValues
                    (
                        cache?.InverterLineVoltageL12 ?? inverter?.AcVoltageL12,
                        cache?.InverterLineVoltageL23 ?? inverter?.AcVoltageL23,
                        cache?.InverterLineVoltageL31 ?? inverter?.AcVoltageL31,
                        cache?.InverterLineVoltageAverage ?? inverter?.AcLineVoltageAverage,
                        "N1", "V"
                    );

                    break;

                case InverterDisplayMode.AcCurrent:
                    SetL123("Sum");
                    Lcd.Header = Loc.AcCurrent;

                    SetLcdValues
                    (
                        cache?.InverterCurrentL1 ?? inverter?.AcCurrentL1,
                        cache?.InverterCurrentL2 ?? inverter?.AcCurrentL2,
                        cache?.InverterCurrentL3 ?? inverter?.AcCurrentL3,
                        cache?.InverterCurrentSum ?? inverter?.AcCurrentSum,
                        "N3", "A"
                    );

                    break;

                case InverterDisplayMode.AcPowerReal:
                    SetL123("Sum");
                    Lcd.Header = Loc.RealPower;

                    SetLcdValues
                    (
                        cache?.InverterRealPowerL1,
                        cache?.InverterRealPowerL2,
                        cache?.InverterRealPowerL3,
                        cache?.InverterRealPowerSum ?? inverter?.PowerRealSum,
                        "N1", "W"
                    );

                    break;

                case InverterDisplayMode.AcPowerApparent:
                    SetL123("Sum");
                    Lcd.Header = Loc.ApparentPower;

                    SetLcdValues
                    (
                        cache?.InverterApparentPowerL1,
                        cache?.InverterApparentPowerL2,
                        cache?.InverterApparentPowerL3,
                        cache?.InverterApparentPowerSum ?? inverter?.PowerApparentSum,
                        "N1", "W"
                    );

                    break;

                case InverterDisplayMode.AcPowerReactive:
                    SetL123("Sum");
                    Lcd.Header = Loc.ReactivePower;

                    SetLcdValues
                    (
                        cache?.InverterReactivePowerL1,
                        cache?.InverterReactivePowerL2,
                        cache?.InverterReactivePowerL3,
                        cache?.InverterReactivePowerSum ?? inverter?.PowerReactiveSum,
                        "N1", "W"
                    );

                    break;

                case InverterDisplayMode.AcPowerFactor:
                    SetL123("Avg");
                    Lcd.Header = Loc.PowerFactor;

                    SetLcdValues
                    (
                        cache?.InverterPowerFactorL1,
                        cache?.InverterPowerFactorL2,
                        cache?.InverterPowerFactorL3,
                        cache?.InverterPowerFactorAverage ?? inverter?.PowerFactorAverage,
                        "N3", string.Empty
                    );

                    break;

                case InverterDisplayMode.DcVoltage:
                    SetDc123(string.Empty);
                    Lcd.Header = Loc.DcVoltage;

                    SetLcdValues
                    (
                        cache?.Solar1Voltage ?? inverter?.Solar1Voltage,
                        cache?.Solar2Voltage ?? inverter?.Solar2Voltage,
                        null,
                        cache?.StorageVoltageOuter ?? inverter?.StorageVoltage,
                        "N1", "V", string.Empty
                    );

                    break;

                case InverterDisplayMode.DcCurrent:
                    SetDc123(string.Empty);
                    Lcd.Header = Loc.DcCurrent;

                    SetLcdValues
                    (
                        cache?.Solar1Current ?? inverter?.Solar1Current,
                        cache?.Solar2Current ?? inverter?.Solar2Current,
                        null,
                        cache?.StorageCurrent ?? inverter?.StorageCurrent ?? gen24?.Storage?.Current,
                        "N3", "A"
                    );

                    break;

                case InverterDisplayMode.DcPower:
                    SetDc123("PvSum");
                    Lcd.Header = Loc.DcPower;

                    SetLcdValues
                    (
                        cache?.Solar1Power ?? inverter?.Solar1Power,
                        cache?.Solar2Power ?? inverter?.Solar2Power,
                        cache?.SolarPowerSum ?? inverter?.SolarPowerSum,
                        cache?.StoragePower ?? inverter?.StoragePower,
                        "N1", "W"
                    );

                    break;

                case InverterDisplayMode.EnergyInverter:
                    Lcd.Header = $"{Loc.Inverter} (kWh)";
                    SetL123("Sum");

                    SetLcdValues
                    (
                        cache?.InverterEnergyProducedL1 / 1000,
                        cache?.InverterEnergyProducedL2 / 1000,
                        cache?.InverterEnergyProducedL3 / 1000,
                        (cache?.InverterEnergyProducedSum ?? dataManager?.InverterLifeTimeEnergyProduced) / 1000,
                        "N3", null, string.Empty
                    );

                    break;

                case InverterDisplayMode.EnergyStorage:
                    Lcd.Header = $"{Loc.Storage} (kWh)";
                    Lcd.Label1 = "In";
                    Lcd.Label2 = "Out";
                    Lcd.Label3 = Lcd.Value3 = string.Empty;
                    Lcd.LabelSum = "Eff";
                    Lcd.Value1 = ToLcd(cache?.StorageLifeTimeEnergyCharged / 1000, "N3");
                    Lcd.Value2 = ToLcd(cache?.StorageLifeTimeEnergyDischarged / 1000, "N3");
                    Lcd.ValueSum = ToLcd(cache?.StorageEfficiency, "P2");
                    break;

                case InverterDisplayMode.EnergySolar:
                    Lcd.Header = $"{Loc.SolarPanels} (kWh)";
                    Lcd.Label1 = "PV1";
                    Lcd.Label2 = "PV2";
                    Lcd.Label3 = "Sum";
                    Lcd.LabelSum = "Eff";
                    Lcd.Value1 = ToLcd(cache?.Solar1EnergyLifeTime / 1000, "N3");
                    Lcd.Value2 = ToLcd(cache?.Solar2EnergyLifeTime / 1000, "N3");
                    Lcd.Value3 = ToLcd(cache?.SolarEnergyLifeTimeSum / 1000, "N3");
                    Lcd.ValueSum = ToLcd((cache?.InverterEnergyProducedSum ?? dataManager?.InverterLifeTimeEnergyProduced) / cache?.SolarEnergyLifeTimeSum, "P2");
                    break;

                case InverterDisplayMode.More:
                    Lcd.Header = dataManager?.SerialNumber;
                    Lcd.Value1 = ToLcd(cache?.InverterFrequency, "N3", "Hz");
                    Lcd.Label1 = "Inv";
                    Lcd.Value2 = ToLcd(cache?.FeedInPointFrequency, "N3", "Hz");
                    Lcd.Label2 = "Grid";
                    Lcd.Value3 = $"{cache?.DataTime?.ToLocalTime():d}";
                    Lcd.Label3 = "Dat";
                    Lcd.ValueSum = $"{cache?.DataTime?.ToLocalTime():T}";
                    Lcd.LabelSum = "Tim";
                    break;

                case InverterDisplayMode.MoreEfficiency:
                    Lcd.Header = Loc.Efficiency;
                    Lcd.Label1 = "Loss";
                    Lcd.Value1 = ToLcd(cache?.SolarPowerSum + (cache?.StoragePower ?? 0) - cache?.InverterRealPowerSum, "N1", "W");
                    Lcd.Label2 = "Eff";
                    Lcd.Value2 = ToLcd(cache?.InverterRealPowerSum / (cache?.SolarPowerSum + (cache?.StoragePower ?? 0)), "P2");
                    Lcd.Label3 = "Sc";
                    Lcd.Value3 = ToLcd(Math.Max(Math.Min(-powerFlow?.LoadPower / powerFlow?.InverterAcPower ?? 0, 1), 0), "P2");
                    Lcd.LabelSum = "Aut";
                    Lcd.ValueSum = ToLcd(powerFlow?.LoadPower > 0 ? 1 : Math.Max(Math.Min(-powerFlow?.InverterAcPower / powerFlow?.LoadPower ?? 0, 1d), 0), "P2");
                    break;

                case InverterDisplayMode.MoreTemperatures:
                    Lcd.Header = Loc.Temperature;
                    Lcd.Label1 = "Ambient";
                    Lcd.Label2 = "Module1";
                    Lcd.Label3 = "Module3";
                    Lcd.LabelSum = "Module4";
                    Lcd.Value1 = ToLcd(cache?.InverterAmbientTemperature, "N1", "°C");
                    Lcd.Value2 = ToLcd(cache?.TemperatureInverterModule1, "N1", "°C");
                    Lcd.Value3 = ToLcd(cache?.TemperatureInverterModule3, "N1", "°C");
                    Lcd.ValueSum = ToLcd(cache?.TemperatureInverterModule4, "N1", "°C");
                    break;

                case InverterDisplayMode.MoreFans:
                    Lcd.Header = Loc.Misc;
                    Lcd.Label1 = "Fan 1";
                    Lcd.Label2 = "Fan 2";
                    Lcd.LabelSum = "Iso";
                    Lcd.Label3 = Lcd.Value3 = string.Empty;
                    Lcd.Value1 = ToLcd(cache?.Fan1RelativeRpm, "P2");
                    Lcd.Value2 = ToLcd(cache?.Fan2RelativeRpm, "P2");
                    Lcd.ValueSum = ToLcd(cache?.IsolatorResistance / 1000, "N0", "kΩ");
                    break;

                case InverterDisplayMode.MoreOp:
                    Lcd.Header = Loc.OpTime;
                    Lcd.Label1 = "Total";
                    Lcd.Label2 = "Backup";
                    Lcd.Label3 = Lcd.Value3 = string.Empty;
                    Lcd.LabelSum = "Bkp";
                    Lcd.Value1 = ToLcd(inverter?.DeviceUpTime?.TotalDays, "N4");
                    Lcd.Value2 = ToLcd(inverter?.BackupModeUpTime?.TotalDays, "N4");
                    Lcd.ValueSum = ToLcd(inverter?.BackupModeUpTime / inverter?.DeviceUpTime, "P5", "%");
                    break;

                case InverterDisplayMode.MoreVersions:
                    VersionList.Visibility = Visibility.Visible;
                    Lcd.Visibility = Visibility.Collapsed;
                    break;
            }
        });
    }

    private void SetLcdValues(double? value1, double? value2, double? value3, double? aggregatedValue, string format, string? unit, string nullValue = "---")
    {
        Lcd.Value1 = ToLcd(value1, format, unit);
        Lcd.Value2 = ToLcd(value2, format, unit);
        Lcd.Value3 = ToLcd(value3, format, unit);
        Lcd.ValueSum = ToLcd(aggregatedValue, format, unit, nullValue);
    }

    private static string ToLcd(double? value, string format, string? unit = null, string nullValue = "---")
    {
        return value.HasValue ? value.Value.ToString(format, CultureInfo.CurrentCulture) + (unit is not null ? ' ' + unit : string.Empty) : nullValue;
    }

    private void SetL123(string sumText) => IHaveLcdPanel.SetL123(Lcd, sumText);
    private void SetTwoPhases(string sumText) => IHaveLcdPanel.SetTwoPhases(Lcd, sumText);

    private void SetDc123(string sumText)
    {
        Lcd.Label1 = "PV1";
        Lcd.Label2 = "PV2";
        Lcd.Label3 = sumText;
        Lcd.LabelSum = "Bat";
    }

    private void CycleMode(IReadOnlyList<InverterDisplayMode> modeList, ref int index)
    {
        index = modeList.Contains(Mode) ? ++index % modeList.Count : 0;
        Mode = modeList[index];
    }

    private void OnAcClicked(object sender, RoutedEventArgs e) => CycleMode(acModes, ref currentAcIndex);

    private void OnDcClicked(object sender, RoutedEventArgs e) => CycleMode(dcModes, ref currentDcIndex);

    private void OnEnergyClicked(object sender, RoutedEventArgs e) => CycleMode(energyModes, ref energyIndex);

    private void OnMoreClicked(object sender, RoutedEventArgs e) => CycleMode(moreModes, ref currentMoreIndex);
}
