using De.Hochstaetter.Fronius.Services;
using Newtonsoft.Json;

namespace De.Hochstaetter.Fronius.Models.Charging;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public partial class WattPilot : BindableBase, IHaveDisplayName, IHaveUniqueId, ICloneable
{
    public bool IsUpdating
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(IsPresent));
            if
            (
                !value ||
                ElectricityPrices == null ||
                IoC.TryGetRegistered<IElectricityPriceService>() is not WattPilotElectricityService wattPilotElectricityPriceService
            )
            {
                return;
            }

            wattPilotElectricityPriceService.PriceRegion = EnergyPriceCountry;
            wattPilotElectricityPriceService.RawValues = ElectricityPrices;
        });
    }

    [ObservableProperty]
    [FroniusProprietaryImport("serial", FroniusDataType.Root)]
    [WattPilot("sse")]
    public partial string? SerialNumber { get; set; }

    [ObservableProperty]
    [WattPilot("rde", false)]
    public partial bool? SendRfidSerialToCloud { get; set; }

    [ObservableProperty]
    [WattPilot("cwe", false)]
    public partial bool? CloudWebSocketEnabled { get; set; }

    [ObservableProperty]
    [WattPilot("lri")]
    public partial string? LastRfidSerial { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("hostname", FroniusDataType.Root)]
    [WattPilot("ffna")]
    public partial string? HostName { get; set; }

    [ObservableProperty]
    [WattPilot("acs", false)]
    public partial AccessMode? AccessMode { get; set; }

    [ObservableProperty]
    [WattPilot("cus")]
    public partial CableLockStatus? CableLockStatus { get; set; }

    [ObservableProperty]
    [WattPilot("ffb")]
    public partial CableLockFeedback? CableLockFeedback { get; set; }

    [ObservableProperty]
    [WattPilot("lmo", false)]
    public partial ChargingLogic? ChargingLogic { get; set; }

    [ObservableProperty]
    [WattPilot("fna", false)]
    [FroniusProprietaryImport("friendly_name", FroniusDataType.Root)]
    public partial string? DeviceName { get; set; }

    [ObservableProperty]
    [WattPilot("wan", false)]
    public partial string? WattPilotSsid { get; set; }

    [ObservableProperty]
    [WattPilot("wak", false)]
    public partial string? WifiPassword { get; set; }

    [ObservableProperty]
    [WattPilot("wen", false)]
    public partial bool? IsWifiClientEnabled { get; set; }

    public bool IsPresent => IsUpdating;

    [ObservableProperty]
    [WattPilot("oem")]
    [FroniusProprietaryImport("manufacturer", FroniusDataType.Root)]
    public partial string? Manufacturer { get; set; }

    [ObservableProperty]
    [WattPilot("typ")]
    [FroniusProprietaryImport("devicetype", FroniusDataType.Root)]
    public partial string? Model { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("version", FroniusDataType.Root)]
    [WattPilot("fwv")]
    public partial Version? Version { get; set; }

    [ObservableProperty]
    [WattPilot("onv")]
    public partial Version? LatestVersion { get; set; }

    [ObservableProperty]
    [WattPilot("cci")]
    public partial WattPilotInverter? Inverter { get; set; }

    [ObservableProperty]
    [JsonProperty("map")]
    [NotifyPropertyChangedFor(nameof(PhaseMap))]
    [WattPilot("map", false, typeof(byte[]))]
    public partial byte[]? Map { get; set; }

    public WattPilotPhaseMap? PhaseMap
    {
        get => Map == null ? null : new(Map[0], Map[1], Map[2]);
        set => Map = value == null ? null : [value.L1Map, value.L2Map, value.L3Map];
    }

    [WattPilot("rssi")]
    public int? WifiSignal
    {
        get;
        set => Set(ref field, value, () =>
        {
            if (CurrentWifi != null)
            {
                CurrentWifi.WifiSignal = value;
            }
        });
    }

    [ObservableProperty]
    [WattPilot("scas")]
    public partial WifiScanStatus WifiScanStatus { get; set; }

    [ObservableProperty]
    [WattPilot("wsms")]
    public partial WifiState WifiState { get; set; }

    [ObservableProperty]
    [WattPilot("scan")]
    public partial List<WattPilotWifiInfo>? ScannedWifis { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("protocol", FroniusDataType.Root)]
    public partial int? Protocol { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("secured", FroniusDataType.Root)]
    public partial bool? IsSecured { get; set; }

    [ObservableProperty]
    [WattPilot("tma", 0)]
    public partial double? TemperatureConnector { get; set; }

    [ObservableProperty]
    [WattPilot("tma", 1)]
    public partial double? TemperatureBoard { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VoltageAverage), nameof(MaximumChargingPowerPossibleSum), nameof(MaximumChargingPowerPossibleL1))]
    [WattPilot("nrg", 0)]
    public partial double? VoltageL1 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VoltageAverage), nameof(MaximumChargingPowerPossibleSum), nameof(MaximumChargingPowerPossibleL2))]
    [WattPilot("nrg", 1)]
    public partial double? VoltageL2 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VoltageAverage), nameof(MaximumChargingPowerPossibleSum), nameof(MaximumChargingPowerPossibleL3))]
    [WattPilot("nrg", 2)]
    public partial double? VoltageL3 { get; set; }

    [ObservableProperty]
    [WattPilot("nrg", 3)]
    public partial double? VoltageL0 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentSum))]
    [WattPilot("nrg", 4)]
    public partial double? CurrentL1 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentSum))]
    [WattPilot("nrg", 5)]
    public partial double? CurrentL2 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentSum))]
    [WattPilot("nrg", 6)]
    public partial double? CurrentL3 { get; set; }

    public double? CurrentSum => CurrentL1 + CurrentL2 + CurrentL3;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PowerSum), nameof(PowerL1KiloWatts), nameof(PowerSumKiloWatts), nameof(ChargingPhases))]
    [WattPilot("nrg", 7)]
    public partial double? PowerL1 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PowerSum), nameof(PowerL2KiloWatts), nameof(PowerSumKiloWatts), nameof(ChargingPhases))]
    [WattPilot("nrg", 8)]
    public partial double? PowerL2 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PowerSum), nameof(PowerL3KiloWatts), nameof(PowerSumKiloWatts), nameof(ChargingPhases))]
    [WattPilot("nrg", 9)]
    public partial double? PowerL3 { get; set; }

    [ObservableProperty]
    [WattPilot("nrg", 10)]
    public partial double? PowerL0 { get; set; }

    public double PowerSum => (PowerL1 ?? 0) + (PowerL2 ?? 0) + (PowerL3 ?? 0);

    public double? PowerL1KiloWatts => PowerL1 / 1000d;
    public double? PowerL2KiloWatts => PowerL2 / 1000d;
    public double? PowerL3KiloWatts => PowerL3 / 1000d;

    public double PowerSumKiloWatts => (PowerL1KiloWatts ?? 0) + (PowerL2KiloWatts ?? 0) + (PowerL3KiloWatts ?? 0);

    public double? VoltageAverage => (VoltageL1 + VoltageL2 + VoltageL3) / 3;

    [ObservableProperty]
    [WattPilot("nrg", 11)]
    public partial double? PowerTotal { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PowerFactorL1))]
    [WattPilot("nrg", 12)]
    public partial double? PowerFactorPercentL1 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PowerFactorL2))]
    [WattPilot("nrg", 13)]
    public partial double? PowerFactorPercentL2 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PowerFactorL3))]
    [WattPilot("nrg", 14)]
    public partial double? PowerFactorPercentL3 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PowerFactorL0))]
    [WattPilot("nrg", 15)]
    public partial double? PowerFactorPercentN { get; set; }

    public double? PowerFactorL1 => PowerFactorPercentL1 / 100;
    public double? PowerFactorL2 => PowerFactorPercentL2 / 100;
    public double? PowerFactorL3 => PowerFactorPercentL3 / 100;
    public double? PowerFactorL0 => PowerFactorPercentN / 100;

    [ObservableProperty]
    [WattPilot("pha", 0)]
    public partial bool? L1ChargerEnabled { get; set; }

    [ObservableProperty]
    [WattPilot("pha", 1)]
    public partial bool? L2ChargerEnabled { get; set; }

    [ObservableProperty]
    [WattPilot("pha", 2)]
    public partial bool? L3ChargerEnabled { get; set; }

    [ObservableProperty]
    [WattPilot("pha", 3)]
    public partial bool? L1CableEnabled { get; set; }

    [ObservableProperty]
    [WattPilot("pha", 4)]
    public partial bool? L2CableEnabled { get; set; }

    [ObservableProperty]
    [WattPilot("pha", 5)]
    public partial bool? L3CableEnabled { get; set; }

    /// <summary>
    ///     Only updated when in Eco mode
    /// </summary>
    [ObservableProperty]
    [WattPilot("pnp")]
    public partial byte? NumberOfCarPhases { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusDisplayName))]
    [WattPilot("modelStatus")]
    public partial ModelStatus? Status { get; set; }

    public string? StatusDisplayName => Status?.ToDisplayName();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusInternalDisplayName))]
    [WattPilot("msi")]
    public partial ModelStatus? StatusInternal { get; set; }

    public string? StatusInternalDisplayName => StatusInternal?.ToDisplayName();

    /// <summary>
    ///     Normally 6A. Can be changed to a higher value if your car can not handle 6A
    /// </summary>
    [ObservableProperty]
    [WattPilot("mca", false)]
    public partial byte? MinimumChargingCurrent { get; set; }

    /// <summary>
    ///     Charge the car at least every amount of milliseconds. Use this if you car disconnects after it has not been charged
    ///     for a while. Set to 0 to disable
    /// </summary>
    [ObservableProperty]
    [WattPilot("mci", false)]
    public partial int? MinimumChargingInterval { get; set; }

    /// <summary>
    ///     Charging pause duration in milliseconds. Some cars need a minimum pause duration before charging can continue. Set
    ///     to 0 to disable.
    /// </summary>
    [ObservableProperty]
    [WattPilot("mcpd", false)]
    public partial int? MinimumPauseDuration { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllowPauseAndHasPhaseSwitch))]
    [WattPilot("fap", false)] //go-eCharger uses "acp" for this
    public partial bool? AllowChargingPause { get; set; }

    /// <summary>
    ///     Used to get the correct market price for energy
    /// </summary>
    [ObservableProperty]
    [WattPilot("awc", false)]
    public partial AwattarCountry EnergyPriceCountry { get; set; }

    /// <summary>
    ///     Maximum energy price to allow charging in ct/kWh
    /// </summary>
    [ObservableProperty]
    [WattPilot("awp", false)]
    public partial float? MaxEnergyPrice { get; set; }

    /// <summary>
    ///     Some cars need this. If yours does not, leave it to false
    /// </summary>
    [ObservableProperty]
    [WattPilot("su", false)]
    public partial bool? SimulateUnplugging { get; set; }

    /// <summary>
    ///     Unclear, what this is good for. The app only uses <see cref="SimulateUnplugging" />
    /// </summary>
    [ObservableProperty]
    [WattPilot("sua", false)]
    public partial bool? SimulateUnpluggingAlways { get; set; }

    /// <summary>
    ///     Minimum time before a phase switch occurs in milliseconds
    /// </summary>
    [ObservableProperty]
    [WattPilot("mptwt", false)]
    public partial int? MinimumTimeBetweenPhaseSwitches { get; set; }

    /// <summary>
    ///     Minimum time in milliseconds the PV surplus must be above or below <see cref="PhaseSwitchPower" /> before the phase
    ///     switch is requested
    /// </summary>
    [ObservableProperty]
    [WattPilot("mpwst", false)]
    public partial int? PhaseSwitchTriggerTime { get; set; }

    /// <summary>
    ///     If PV surplus is above that power, switch to 3 phases. If PV surplus is below, switch to 1 phase.
    ///     Use <see cref="PhaseSwitchTriggerTime" /> to control the minimum time before the phase switch occurs.
    /// </summary>
    [WattPilot("spl3", false)]
    [ObservableProperty]
    public partial float? PhaseSwitchPower { get; set; }

    public bool? AllowPauseAndHasPhaseSwitch => PhaseSwitchMode == Charging.PhaseSwitchMode.Auto && AllowChargingPause.HasValue && AllowChargingPause.Value;

    /// <summary>
    ///     Next trip departure time in seconds from day start (local-time)
    /// </summary>
    [ObservableProperty]
    [WattPilot("ftt", false)]
    public partial int? NextTripTime { get; set; }

    /// <summary>
    ///     Next trip minimum energy to charge
    /// </summary>
    [ObservableProperty]
    [WattPilot("fte", false)]
    public partial int? NextTripEnergyToCharge { get; set; }

    [ObservableProperty]
    [WattPilot("fre", false)]
    public partial bool? NextTripRemainInEcoMode { get; set; }

    [ObservableProperty]
    [WattPilot("rsre", false)]
    public partial bool? EnableGridMonitoringOnStartUp { get; set; }

    [ObservableProperty]
    [WattPilot("gmtr", false)]
    public partial int GridMonitoringTimeOnStartUp { get; set; }

    [ObservableProperty]
    [WattPilot("rmiv", false)]
    public partial float? StartUpMonitoringMinimumVoltage { get; set; }

    [ObservableProperty]
    [WattPilot("rmav", false)]
    public partial float? StartUpMonitoringMaximumVoltage { get; set; }

    [ObservableProperty]
    [WattPilot("rmif", false)]
    public partial float? StartUpMonitoringMinimumFrequency { get; set; }

    [ObservableProperty]
    [WattPilot("rmaf", false)]
    public partial float? StartUpMonitoringMaximumFrequency { get; set; }

    [ObservableProperty]
    [WattPilot("rsrr", false)]
    public partial float? StartUpRampUpRate { get; set; }

    /// <summary>
    ///     Set this in accordance with your DNO, e.g. don´t use 32 A if your house only supports 35A
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor
    (
        nameof(MaximumChargingCurrentPossible), nameof(MaximumChargingPowerPossibleSum), nameof(MaximumChargingPowerPossibleL1),
        nameof(MaximumChargingPowerPossibleL2), nameof(MaximumChargingPowerPossibleL3), nameof(MaximumChargingCurrentPossiblePerPhase)
    )]
    [WattPilot("ama", false)]
    public partial byte? AbsoluteMaximumChargingCurrent { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor
    (
        nameof(MaximumChargingCurrentPossible), nameof(MaximumChargingPowerPossibleSum), nameof(MaximumChargingPowerPossibleL1),
        nameof(MaximumChargingPowerPossibleL2), nameof(MaximumChargingPowerPossibleL3), nameof(MaximumChargingCurrentPossiblePerPhase)
    )]
    [WattPilot("la1", false)]
    public partial byte? MaximumChargingCurrentPhase1 { get; set; }

    [WattPilot("la3", false)]
    public byte? MaximumChargingCurrentPhase3
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets whether you want the cable to unlock if the WattPilot is not powered.
    /// </summary>
    [ObservableProperty]
    [WattPilot("upo", false)]
    public partial bool? UnlockCableOnPowerFailure { get; set; }

    /// <summary>
    ///     If you don´t have a house battery, you can specify whether you prefer power from/to grid. If you have a battery,
    ///     this setting does not make a big difference
    /// </summary>
    [ObservableProperty]
    [WattPilot("frm", false)]
    public partial EcoRoundingMode RoundingMode { get; set; }

    /// <summary>
    ///     Minimum power in Watts to start PV surplus charging
    /// </summary>
    [ObservableProperty]
    [WattPilot("fst", false)]
    public partial float? PvSurplusPowerThreshold { get; set; }

    /// <summary>
    ///     Once charging has started, it continues for at least <see cref="MinimumChargingTime" /> milliseconds.
    /// </summary>
    [ObservableProperty]
    [WattPilot("fmt", false)]
    public partial int? MinimumChargingTime { get; set; }

    /// <summary>
    ///     The current active charging limit. Must be between <see cref="MinimumChargingCurrent" /> and
    ///     <see cref="AbsoluteMaximumChargingCurrent" />
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaximumChargingCurrentPossible),nameof(MaximumChargingCurrentPossiblePerPhase),nameof(MaximumChargingPowerPossibleSum))]
    [WattPilot("amp", false)]
    public partial byte? MaximumChargingCurrent { get; set; }

    public byte ChargingPhases
    {
        get
        {
            var phases = (byte)new[] { PowerL1, PowerL2, PowerL3 }.Count(p => p is > 10);

            if (phases == 0)
            {
                phases = PhaseSwitchMode == Charging.PhaseSwitchMode.Phase1 ? (byte)1 : (byte)3;
            }

            return phases;
        }
    }

    public byte MaximumChargingCurrentPossible => (byte)(MaximumChargingCurrentPossiblePerPhase * ChargingPhases);

    public double MaximumChargingPowerPossibleSum
    {
        get
        {
            var voltages = new[] { VoltageL1 ?? 0, VoltageL2 ?? 0, VoltageL3 ?? 0 }[..Math.Max(ChargingPhases, (byte)1)];
            var result = MaximumChargingCurrentPossible * voltages.Average();
            return result;
        }
    }

    public double? MaximumChargingPowerPossibleL1 => VoltageL1 * MaximumChargingCurrentPossiblePerPhase;
    public double? MaximumChargingPowerPossibleL2 => VoltageL2 * MaximumChargingCurrentPossiblePerPhase;
    public double? MaximumChargingPowerPossibleL3 => VoltageL3 * MaximumChargingCurrentPossiblePerPhase;

    public byte MaximumChargingCurrentPossiblePerPhase
    {
        get
        {
            var result = MaximumChargingCurrent;

            if (AbsoluteMaximumChargingCurrent < result)
            {
                result = AbsoluteMaximumChargingCurrent;
            }

            if (ChargingPhases == 1 && MaximumChargingCurrentPhase1 < result)
            {
                result = MaximumChargingCurrentPhase1;
            }

            if (CableCurrentMaximum < result)
            {
                result = CableCurrentMaximum;
            }

            return result ?? 0;
        }
    }

    [ObservableProperty]
    [WattPilot("dll")]
    public partial string? DownloadLink { get; set; }

    /// <summary>
    ///     Token to be used at https://&lt;serial number&gt;.api.v3.go-e.io/
    ///     Requires that <see cref="CloudAccessEnabled" /> is true
    /// </summary>
    [ObservableProperty]
    [WattPilot("cak", false)]
    public partial string? CloudAccessKey { get; set; }

    /// <summary>
    ///     True: Can use go-E charger Cloud Api
    /// </summary>
    [ObservableProperty]
    [WattPilot("cae", false)]
    public partial bool? CloudAccessEnabled { get; set; }

    [ObservableProperty]
    [WattPilot("car")]
    public partial CarStatus? CarStatus { get; set; }

    /// <summary>
    ///     Read-Only: is null when no car is connected
    /// </summary>
    [ObservableProperty]
    [WattPilot("acu")]
    public partial double? AllowedCurrent { get; set; }

    [ObservableProperty]
    [WattPilot("acui")]
    public partial double? AllowedCurrentInternal { get; set; }

    /// <summary>
    ///     Read-only: All prerequisites met (RFID auth, <see cref="ButtonEnableCurrentSelection" />, etc.)
    /// </summary>
    [ObservableProperty]
    [WattPilot("alw")]
    public partial bool? IsChargingAllowed { get; set; }

    /// <summary>
    ///     Read-write: Allow/Disallow changing the current levels via button
    /// </summary>
    [ObservableProperty]
    [WattPilot("bac", false)]
    public partial bool? ButtonEnableCurrentSelection { get; set; }

    [ObservableProperty]
    [WattPilot("bam", false)]
    public partial bool ButtonEnableModeSwitch { get; set; }

    [ObservableProperty]
    [WattPilot("frc", false)]
    public partial ForcedCharge ForcedCharge { get; set; }

    /// <summary>
    ///     Total energy delivered by the charger. Includes energy of current charging session
    /// </summary>
    [ObservableProperty]
    [WattPilot("eto")]
    public partial double? TotalEnergy { get; set; }

    /// <summary>
    ///     Total energy delivered by the charger. Does not include energy of current charging session
    /// </summary>
    [ObservableProperty]
    [WattPilot("etop")]
    public partial double? TotalEnergyWithoutCurrentSession { get; set; }

    [ObservableProperty]
    [WattPilot("wh")]
    public partial double? EnergyCurrentSession { get; set; }

    /// <summary>
    ///     Maximum current that the connected cable supports.
    /// </summary>
    [ObservableProperty]
    [WattPilot("cbl")]
    [NotifyPropertyChangedFor(nameof(MaximumChargingCurrentPossible),nameof(MaximumChargingCurrentPossiblePerPhase),nameof(MaximumChargingPowerPossibleSum))]
    public partial byte? CableCurrentMaximum { get; set; }

    /// <summary>
    ///     Maximum Power that the WattPilot is able to deliver (11 or 22 kW)
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaximumWattPilotPower))]
    [WattPilot("var")]
    public partial byte? MaximumWattPilotPowerKiloWatts { get; set; }

    public double? MaximumWattPilotPower => MaximumWattPilotPowerKiloWatts * 1000d;

    [ObservableProperty]
    [NotifyPropertyChangedFor
    (
        nameof(AllowPauseAndHasPhaseSwitch),nameof(ChargingPhases),nameof(MaximumChargingCurrentPossible),
        nameof(MaximumChargingCurrentPossiblePerPhase),nameof(MaximumChargingPowerPossibleSum)
    )]
    [WattPilot("psm", false)]
    public partial PhaseSwitchMode? PhaseSwitchMode { get; set; }

    /// <summary>
    ///     If your inverter does not feed the grid, enable this
    /// </summary>
    [ObservableProperty]
    [WattPilot("fzf", false)]
    public partial bool? NoFeedIn { get; set; }

    [ObservableProperty]
    [WattPilot("cards")]
    public partial IList<WattPilotCard>? Cards { get; set; }

    [WattPilot("awpl")]
    public IList<WattPilotElectricityPrice>? ElectricityPrices
    {
        get;
        set => Set(ref field, value, () =>
        {
            if (IoC.TryGetRegistered<IElectricityPriceService>() is not WattPilotElectricityService wattPilotElectricityPriceService)
            {
                return;
            }

            wattPilotElectricityPriceService.PriceRegion = EnergyPriceCountry;

            if (value != null)
            {
                wattPilotElectricityPriceService.RawValues = value;
            }
        });
    }

    /// <summary>
    ///     null = Unauthenticated, 0=guest charging, 1=card index 0, 2= card index 1, ...
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentUser))]
    [WattPilot("trx", false)]
    public partial byte? AuthenticatedCardIndex { get; set; }

    public string CurrentUser => AuthenticatedCardIndex switch
    {
        0 => Resources.Guest,
        null => Resources.NoRfidCard,
        _ => Cards?[AuthenticatedCardIndex.Value - 1].Name ?? "---",
    };

    /// <summary>
    ///     Is this the 16 Amperes limited version ("Go 11 J" or "Home 11 J")?
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaxWattPilotCurrent))]
    [WattPilot("adi")]
    public partial bool? Is16AmpereVariant { get; set; }

    /// <summary>
    ///     Maximum current that the WattPilot can deliver
    /// </summary>
    public byte? MaxWattPilotCurrent => !Is16AmpereVariant.HasValue ? null : Is16AmpereVariant.Value ? (byte)16 : (byte)32;

    [ObservableProperty]
    [WattPilot("fhz")]
    public partial double? Frequency { get; set; }

    [WattPilot("utc")]
    public DateTime? TimeStampUtc
    {
        get;
        set => Set(ref field, value, () => Latency = DateTime.UtcNow - TimeStampUtc);
    }

    [ObservableProperty]
    public partial TimeSpan? Latency { get; set; }

    /// <summary>
    ///     Used to tune PV surplus charging. See also <see cref="OhmPilotTemperatureLimitCelsius" />
    /// </summary>
    [ObservableProperty]
    [WattPilot("fam", false)]
    public partial int? PvSurplusBatteryLevelStartCharge { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanModifyChargingFromBatteryOptions))]
    [WattPilot("pdte", false)]
    public partial bool? AllowChargingFromBattery { get; set; }

    public bool CanModifyChargingFromBatteryOptions => AllowChargingFromBattery.HasValue && AllowChargingFromBattery.Value && PvSurplusEnabled.HasValue && PvSurplusEnabled.Value;

    [ObservableProperty]
    [WattPilot("pdt", false)]
    public partial byte? PvSurplusBatteryLevelStopCharge { get; set; }

    [ObservableProperty]
    [WattPilot("pdle", false)]
    public partial bool? RestrictChargingFromBattery { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllowChargingFromBatteryStartString))]
    [WattPilot("pdls", false)]
    public partial int? AllowChargingFromBatteryStartSeconds { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllowChargingFromBatteryStopString))]
    [WattPilot("pdlo", false)]
    public partial int? AllowChargingFromBatteryStopSeconds { get; set; }


    public string AllowChargingFromBatteryStartString
    {
        get => $"{AllowChargingFromBatteryStartSeconds / 3600:D2}:{AllowChargingFromBatteryStartSeconds % 3600 / 60:D2}";
        set
        {
            var split = value.Split(':');
            AllowChargingFromBatteryStartSeconds = int.Parse(split[0]) * 3600 + int.Parse(split[1]) * 60;
        }
    }

    public string AllowChargingFromBatteryStopString
    {
        get => $"{AllowChargingFromBatteryStopSeconds / 3600:D2}:{AllowChargingFromBatteryStopSeconds % 3600 / 60:D2}";
        set
        {
            var split = value.Split(':');
            AllowChargingFromBatteryStopSeconds = int.Parse(split[0]) * 3600 + int.Parse(split[1]) * 60;
        }
    }


    [ObservableProperty]
    [WattPilot("ebe", false)]
    public partial bool? EnableBatteryBoost { get; set; }

    [ObservableProperty]
    [WattPilot("ebo", false)]
    public partial bool? EnableSingleTimeBoost { get; set; }

    [ObservableProperty]
    [WattPilot("ebt", false)]
    public partial byte? MinimumSocInBoost { get; set; }

    [ObservableProperty]
    [WattPilot("rst", false)]
    public partial bool? Reboot { get; set; }

    /// <summary>
    ///     Used to tune PV surplus charging. See also <see cref="PvSurplusBatteryLevelStartCharge" />
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OhmPilotTemperatureLimitFahrenheit))]
    [WattPilot("fot", false)]
    public partial int? OhmPilotTemperatureLimitCelsius { get; set; }

    public double? OhmPilotTemperatureLimitFahrenheit
    {
        get => OhmPilotTemperatureLimitCelsius * 1.8 + 32;
        set => OhmPilotTemperatureLimitCelsius = value.HasValue ? (int)Math.Round((value.Value - 32) / 1.8, MidpointRounding.AwayFromZero) : null;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanModifyChargingFromBatteryOptions))]
    [WattPilot("fup", false)]
    public partial bool? PvSurplusEnabled { get; set; }

    [ObservableProperty]
    [WattPilot("ccw", typeof(WattPilotWifiInfo))]
    public partial WattPilotWifiInfo? CurrentWifi { get; set; }

    [ObservableProperty]
    [WattPilot("ful", false)]
    public partial bool? AwattarEnabled { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CableLockBehaviorDisplayName))]
    [WattPilot("ust", false)]
    public partial CableLockBehavior? CableLockBehavior { get; set; }

    [ObservableProperty]
    [WattPilot("nmo", false)]
    public partial bool? DisableProtectiveEarth { get; set; }

    [ObservableProperty]
    [WattPilot("loe", false)]
    public partial bool? LoadBalancingEnabled { get; set; }

    [ObservableProperty]
    [WattPilot("ule", false)]
    public partial bool? EnableOutOfBalanceControl { get; set; }

    [ObservableProperty]
    [WattPilot("ulu", false)]
    public partial bool? ShowOutOfBalanceControlInVoltAmpere { get; set; }

    [ObservableProperty]
    [WattPilot("ula", false)]
    public partial byte? MaximumOutOfBalanceCurrent { get; set; }

    [ObservableProperty]
    [WattPilot("lot", false, typeof(WattPilotLoadBalancingCurrents))]
    public partial WattPilotLoadBalancingCurrents? LoadBalancingCurrents { get; set; }

    [ObservableProperty]
    [WattPilot("lof", false)]
    public partial int? LoadBalancingFallbackCurrent { get; set; }

    [ObservableProperty]
    [WattPilot("lop", false)]
    public partial LoadBalancingPriority LoadBalancingPriority { get; set; }

    [ObservableProperty]
    [WattPilot("rdre", false)]
    public partial int? RandomDelayPowerFailure { get; set; }

    [ObservableProperty]
    [WattPilot("rdbf", false)]
    public partial int? RandomDelayAwattarStart { get; set; }

    [ObservableProperty]
    [WattPilot("rdef", false)]
    public partial int? RandomDelayAwattarStop { get; set; }

    [ObservableProperty]
    [WattPilot("rdbs", false)]
    public partial int? RandomDelayTimerStart { get; set; }

    [ObservableProperty]
    [WattPilot("rdes", false)]
    public partial int? RandomDelayTimerStop { get; set; }

    [ObservableProperty]
    [WattPilot("rdpl", false)]
    public partial int? RandomDelayCarConnect { get; set; }

    public string? CableLockBehaviorDisplayName => CableLockBehavior?.ToDisplayName();

    public string DisplayName => $"{DeviceName ?? HostName ?? SerialNumber ?? Resources.Unknown}";

    public override string ToString() => DisplayName;

    public object Clone()
    {
        var result = (WattPilot)MemberwiseClone();
        result.IsUpdating = false;

        if (Cards != null)
        {
            WattPilotCard[] newCards = new WattPilotCard[Cards.Count];

            for (var i = 0; i < Cards.Count; i++)
            {
                newCards[i] = new WattPilotCard { Energy = Cards[i].Energy, HaveCardId = Cards[i].HaveCardId, Name = Cards[i].Name, };
            }

            result.Cards = newCards;
        }

        if (LoadBalancingCurrents is not null)
        {
            result.LoadBalancingCurrents = LoadBalancingCurrents.Copy();
        }

        return result;
    }

    public static WattPilot Parse(JToken token)
    {
        return IoC.Get<IGen24JsonService>().ReadFroniusData<WattPilot>(token);
    }
}
