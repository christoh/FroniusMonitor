using De.Hochstaetter.Fronius.Services;
using Newtonsoft.Json;

namespace De.Hochstaetter.Fronius.Models.Charging;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class WattPilot : BindableBase, IHaveDisplayName, ICloneable
{
    public bool IsUpdating
    {
        get;
        set => Set(ref field, value, () =>
        {
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

    [FroniusProprietaryImport("serial", FroniusDataType.Root)]
    [WattPilot("sse")]
    public string? SerialNumber
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("rde", false)]
    public bool? SendRfidSerialToCloud
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("cwe", false)]
    public bool? CloudWebSocketEnabled
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("lri")]
    public string? LastRfidSerial
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("hostname", FroniusDataType.Root)]
    [WattPilot("ffna")]
    public string? HostName
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("acs", false)]
    public AccessMode? AccessMode
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("cus")]
    public CableLockStatus? CableLockStatus
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("ffb")]
    public CableLockFeedback? CableLockFeedback
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("lmo", false)]
    public ChargingLogic? ChargingLogic
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("fna", false)]
    [FroniusProprietaryImport("friendly_name", FroniusDataType.Root)]
    public string? DeviceName
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("wan", false)]
    public string? WattPilotSsid
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("wak", false)]
    public string? WifiPassword
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("wen", false)]
    public bool? IsWifiClientEnabled
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("oem")]
    [FroniusProprietaryImport("manufacturer", FroniusDataType.Root)]
    public string? Manufacturer
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("typ")]
    [FroniusProprietaryImport("devicetype", FroniusDataType.Root)]
    public string? Model
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("version", FroniusDataType.Root)]
    [WattPilot("fwv")]
    public Version? Version
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("onv")]
    public Version? LatestVersion
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("cci")]
    public WattPilotInverter? Inverter
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("map", false, typeof(byte[]))]
    [JsonProperty("map")]
    public byte[]? Map
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(PhaseMap)));
    }

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

    [WattPilot("scas")]
    public WifiScanStatus WifiScanStatus
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("wsms")]
    public WifiState WifiState
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("scan")]
    public List<WattPilotWifiInfo>? ScannedWifis
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("protocol", FroniusDataType.Root)]
    public int? Protocol
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("secured", FroniusDataType.Root)]
    public bool? IsSecured
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("tma", 0)]
    public double? TemperatureConnector
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("tma", 1)]
    public double? TemperatureBoard
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("nrg", 0)]
    public double? VoltageL1
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(VoltageAverage));
            NotifyOfPropertyChange(nameof(MaximumChargingPowerPossibleSum));
            NotifyOfPropertyChange(nameof(MaximumChargingPowerPossibleL1));
        });
    }

    [WattPilot("nrg", 1)]
    public double? VoltageL2
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(VoltageAverage));
            NotifyOfPropertyChange(nameof(MaximumChargingPowerPossibleSum));
            NotifyOfPropertyChange(nameof(MaximumChargingPowerPossibleL2));
        });
    }

    [WattPilot("nrg", 2)]
    public double? VoltageL3
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(VoltageAverage));
            NotifyOfPropertyChange(nameof(MaximumChargingPowerPossibleSum));
            NotifyOfPropertyChange(nameof(MaximumChargingPowerPossibleL3));
        });
    }

    [WattPilot("nrg", 3)]
    public double? VoltageL0
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("nrg", 4)]
    public double? CurrentL1
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(CurrentSum)));
    }

    [WattPilot("nrg", 5)]
    public double? CurrentL2
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(CurrentSum)));
    }

    [WattPilot("nrg", 6)]
    public double? CurrentL3
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(CurrentSum)));
    }

    public double? CurrentSum => CurrentL1 + CurrentL2 + CurrentL3;

    [WattPilot("nrg", 7)]
    public double? PowerL1
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(PowerSum));
            NotifyOfPropertyChange(nameof(PowerL1KiloWatts));
            NotifyOfPropertyChange(nameof(PowerSumKiloWatts));
            NotifyOfPropertyChange(nameof(ChargingPhases));
        });
    }

    [WattPilot("nrg", 8)]
    public double? PowerL2
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(PowerSum));
            NotifyOfPropertyChange(nameof(PowerL2KiloWatts));
            NotifyOfPropertyChange(nameof(PowerSumKiloWatts));
            NotifyOfPropertyChange(nameof(ChargingPhases));
        });
    }

    [WattPilot("nrg", 9)]
    public double? PowerL3
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(PowerSum));
            NotifyOfPropertyChange(nameof(PowerL3KiloWatts));
            NotifyOfPropertyChange(nameof(PowerSumKiloWatts));
            NotifyOfPropertyChange(nameof(ChargingPhases));
        });
    }

    [WattPilot("nrg", 10)]
    public double? PowerL0
    {
        get;
        set => Set(ref field, value);
    }

    public double PowerSum => (PowerL1 ?? 0) + (PowerL2 ?? 0) + (PowerL3 ?? 0);

    public double? PowerL1KiloWatts => PowerL1 / 1000d;
    public double? PowerL2KiloWatts => PowerL2 / 1000d;
    public double? PowerL3KiloWatts => PowerL3 / 1000d;

    public double PowerSumKiloWatts => (PowerL1KiloWatts ?? 0) + (PowerL2KiloWatts ?? 0) + (PowerL3KiloWatts ?? 0);

    public double? VoltageAverage => (VoltageL1 + VoltageL2 + VoltageL3) / 3;

    [WattPilot("nrg", 11)]
    public double? PowerTotal
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("nrg", 12)]
    public double? PowerFactorPercentL1
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(PowerFactorL1)));
    }

    [WattPilot("nrg", 13)]
    public double? PowerFactorPercentL2
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(PowerFactorL2)));
    }

    [WattPilot("nrg", 14)]
    public double? PowerFactorPercentL3
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(PowerFactorL3)));
    }

    [WattPilot("nrg", 15)]
    public double? PowerFactorPercentN
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(PowerFactorL0)));
    }

    public double? PowerFactorL1 => PowerFactorPercentL1 / 100;
    public double? PowerFactorL2 => PowerFactorPercentL2 / 100;
    public double? PowerFactorL3 => PowerFactorPercentL3 / 100;
    public double? PowerFactorL0 => PowerFactorPercentN / 100;

    [WattPilot("pha", 0)]
    public bool? L1ChargerEnabled
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("pha", 1)]
    public bool? L2ChargerEnabled
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("pha", 2)]
    public bool? L3ChargerEnabled
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("pha", 3)]
    public bool? L1CableEnabled
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("pha", 4)]
    public bool? L2CableEnabled
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("pha", 5)]
    public bool? L3CableEnabled
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Only updated when in Eco mode
    /// </summary>
    [WattPilot("pnp")]
    public byte? NumberOfCarPhases
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("modelStatus")]
    public ModelStatus? Status
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(StatusDisplayName)));
    }

    public string? StatusDisplayName => Status?.ToDisplayName();

    [WattPilot("msi")]
    public ModelStatus? StatusInternal
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(StatusInternalDisplayName)));
    }

    public string? StatusInternalDisplayName => StatusInternal?.ToDisplayName();

    /// <summary>
    ///     Normally 6A. Can be changed to a higher value if your car can not handle 6A
    /// </summary>
    [WattPilot("mca", false)]
    public byte? MinimumChargingCurrent
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Charge the car at least every amount of milliseconds. Use this if you car disconnects after it has not been charged
    ///     for a while. Set to 0 to disable
    /// </summary>
    [WattPilot("mci", false)]
    public int? MinimumChargingInterval
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Charging pause duration in milliseconds. Some cars need a minimum pause duration before charging can continue. Set
    ///     to 0 to disable.
    /// </summary>
    [WattPilot("mcpd", false)]
    public int? MinimumPauseDuration
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("fap", false)] //go-eCharger uses "acp" for this
    public bool? AllowChargingPause
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(AllowPauseAndHasPhaseSwitch)));
    }

    /// <summary>
    ///     Used to get the correct market price for energy
    /// </summary>
    [WattPilot("awc", false)]
    public AwattarCountry EnergyPriceCountry
    {
        get;

        set => Set(ref field, value, () =>
        {
            //if (IoC.TryGetRegistered<IElectricityPriceService>() is WattPilotElectricityService wattPilotElectricityPriceService)
            //{
            //    wattPilotElectricityPriceService.PriceZone = value;
            //}
        });
    }

    /// <summary>
    ///     Maximum energy price to allow charging in ct/kWh
    /// </summary>
    [WattPilot("awp", false)]
    public float? MaxEnergyPrice
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Some cars need this. If yours does not, leave it to false
    /// </summary>
    [WattPilot("su", false)]
    public bool? SimulateUnplugging
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Unclear, what this is good for. The app only uses <see cref="SimulateUnplugging" />
    /// </summary>
    [WattPilot("sua", false)]
    public bool? SimulateUnpluggingAlways
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Minimum time before a phase switch occurs in milliseconds
    /// </summary>
    [WattPilot("mptwt", false)]
    public int? MinimumTimeBetweenPhaseSwitches
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Minimum time in milliseconds the PV surplus must be above or below <see cref="PhaseSwitchPower" /> before the phase
    ///     switch is requested
    /// </summary>
    [WattPilot("mpwst", false)]
    public int? PhaseSwitchTriggerTime
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     If PV surplus is above that power, switch to 3 phases. If PV surplus is below, switch to 1 phase.
    ///     Use <see cref="PhaseSwitchTriggerTime" /> to control the minimum time before the phase switch occurs.
    /// </summary>
    [WattPilot("spl3", false)]
    public float? PhaseSwitchPower
    {
        get;
        set => Set(ref field, value);
    }

    public bool? AllowPauseAndHasPhaseSwitch => PhaseSwitchMode == Charging.PhaseSwitchMode.Auto && AllowChargingPause.HasValue && AllowChargingPause.Value;

    /// <summary>
    ///     Next trip departure time in seconds from day start (local-time)
    /// </summary>
    [WattPilot("ftt", false)]
    public int? NextTripTime
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Next trip minimum energy to charge
    /// </summary>
    [WattPilot("fte", false)]
    public int? NextTripEnergyToCharge
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("fre", false)]
    public bool? NextTripRemainInEcoMode
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("rsre", false)]
    public bool? EnableGridMonitoringOnStartUp
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("gmtr", false)]
    public int GridMonitoringTimeOnStartUp
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("rmiv", false)]
    public float? StartUpMonitoringMinimumVoltage
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("rmav", false)]
    public float? StartUpMonitoringMaximumVoltage
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("rmif", false)]
    public float? StartUpMonitoringMinimumFrequency
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("rmaf", false)]
    public float? StartUpMonitoringMaximumFrequency
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("rsrr", false)]
    public float? StartUpRampUpRate
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Set this in accordance with your DNO, e.g. don´t use 32 A if your house only supports 35A
    /// </summary>
    [WattPilot("ama", false)]
    public byte? AbsoluteMaximumChargingCurrent
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(MaximumChargingCurrentPossible));
            NotifyOfPropertyChange(nameof(MaximumChargingPowerPossibleSum));
            NotifyOfPropertyChange(nameof(MaximumChargingPowerPossibleL1));
            NotifyOfPropertyChange(nameof(MaximumChargingPowerPossibleL2));
            NotifyOfPropertyChange(nameof(MaximumChargingPowerPossibleL3));
            NotifyOfPropertyChange(nameof(MaximumChargingCurrentPossiblePerPhase));
        });
    }

    [WattPilot("la1", false)]
    public byte? MaximumChargingCurrentPhase1
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(MaximumChargingCurrentPossible));
            NotifyOfPropertyChange(nameof(MaximumChargingPowerPossibleSum));
            NotifyOfPropertyChange(nameof(MaximumChargingPowerPossibleL1));
            NotifyOfPropertyChange(nameof(MaximumChargingPowerPossibleL2));
            NotifyOfPropertyChange(nameof(MaximumChargingPowerPossibleL3));
            NotifyOfPropertyChange(nameof(MaximumChargingCurrentPossiblePerPhase));
        });
    }

    [WattPilot("la3", false)]
    public byte? MaximumChargingCurrentPhase3
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets whether you want the cable to unlock if the WattPilot is not powered.
    /// </summary>
    [WattPilot("upo", false)]
    public bool? UnlockCableOnPowerFailure
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     If you don´t have a house battery, you can specify whether you prefer power from/to grid. If you have a battery,
    ///     this setting does not make a big difference
    /// </summary>
    [WattPilot("frm", false)]
    public EcoRoundingMode RoundingMode
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Minimum power in Watts to start PV surplus charging
    /// </summary>
    [WattPilot("fst", false)]
    public float? PvSurplusPowerThreshold
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Once charging has started, it continues for at least <see cref="MinimumChargingTime" /> milliseconds.
    /// </summary>
    [WattPilot("fmt", false)]
    public int? MinimumChargingTime
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     The current active charging limit. Must be between <see cref="MinimumChargingCurrent" /> and
    ///     <see cref="AbsoluteMaximumChargingCurrent" />
    /// </summary>
    [WattPilot("amp", false)]
    public byte? MaximumChargingCurrent
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(MaximumChargingCurrentPossible));
            NotifyOfPropertyChange(nameof(MaximumChargingCurrentPossiblePerPhase));
            NotifyOfPropertyChange(nameof(MaximumChargingPowerPossibleSum));
        });
    }

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

    [WattPilot("dll")]
    public string? DownloadLink
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Token to be used at https://&lt;serial number&gt;.api.v3.go-e.io/
    ///     Requires that <see cref="CloudAccessEnabled" /> is true
    /// </summary>
    [WattPilot("cak", false)]
    public string? CloudAccessKey
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     True: Can use go-E charger Cloud Api
    /// </summary>
    [WattPilot("cae", false)]
    public bool? CloudAccessEnabled
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("car")]
    public CarStatus? CarStatus
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Read-Only: is null when no car is connected
    /// </summary>
    [WattPilot("acu")]
    public double? AllowedCurrent
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("acui")]
    public double? AllowedCurrentInternal
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Read-only: All prerequisites met (RFID auth, <see cref="ButtonEnableCurrentSelection" />, etc.)
    /// </summary>
    [WattPilot("alw")]
    public bool? IsChargingAllowed
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Read-write: Allow/Disallow changing the current levels via button
    /// </summary>
    [WattPilot("bac", false)]
    public bool? ButtonEnableCurrentSelection
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("bam", false)]
    public bool ButtonEnableModeSwitch
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("frc", false)]
    public ForcedCharge ForcedCharge
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Total energy delivered by the charger. Includes energy of current charging session
    /// </summary>
    [WattPilot("eto")]
    public double? TotalEnergy
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Total energy delivered by the charger. Does not include energy of current charging session
    /// </summary>
    [WattPilot("etop")]
    public double? TotalEnergyWithoutCurrentSession
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("wh")]
    public double? EnergyCurrentSession
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Maximum current that the connected cable supports.
    /// </summary>
    [WattPilot("cbl")]
    public byte? CableCurrentMaximum
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(MaximumChargingCurrentPossible));
            NotifyOfPropertyChange(nameof(MaximumChargingCurrentPossiblePerPhase));
            NotifyOfPropertyChange(nameof(MaximumChargingPowerPossibleSum));
        });
    }

    /// <summary>
    ///     Maximum Power that the WattPilot is able to deliver (11 or 22 kW)
    /// </summary>
    [WattPilot("var")]
    public byte? MaximumWattPilotPowerKiloWatts
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(MaximumWattPilotPower)));
    }

    public double? MaximumWattPilotPower => MaximumWattPilotPowerKiloWatts * 1000d;

    [WattPilot("psm", false)]
    public PhaseSwitchMode? PhaseSwitchMode
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(AllowPauseAndHasPhaseSwitch));
            NotifyOfPropertyChange(nameof(ChargingPhases));
            NotifyOfPropertyChange(nameof(MaximumChargingCurrentPossible));
            NotifyOfPropertyChange(nameof(MaximumChargingCurrentPossiblePerPhase));
            NotifyOfPropertyChange(nameof(MaximumChargingPowerPossibleSum));
        });
    }

    /// <summary>
    ///     If your inverter does not feed the grid, enable this
    /// </summary>
    [WattPilot("fzf", false)]
    public bool? NoFeedIn
    {
        get;
        set => Set(ref field, value);
    }

    private IList<WattPilotCard>? cards;

    [WattPilot("cards")]
    public IList<WattPilotCard>? Cards
    {
        get => cards;
        set => Set(ref cards, value);
    }

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
    [WattPilot("trx", false)]
    public byte? AuthenticatedCardIndex
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(CurrentUser)));
    }

    public string CurrentUser => AuthenticatedCardIndex switch
    {
        0 => Resources.Guest,
        null => Resources.NoRfidCard,
        _ => Cards?[AuthenticatedCardIndex.Value - 1].Name ?? "---",
    };

    /// <summary>
    ///     Is this the 16 Amperes limited version ("Go 11 J" or "Home 11 J")?
    /// </summary>
    [WattPilot("adi")]
    public bool? Is16AmpereVariant
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(MaxWattPilotCurrent)));
    }

    /// <summary>
    ///     Maximum current that the WattPilot can deliver
    /// </summary>
    public byte? MaxWattPilotCurrent => !Is16AmpereVariant.HasValue ? null : Is16AmpereVariant.Value ? (byte)16 : (byte)32;

    [WattPilot("fhz")]
    public double? Frequency
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("utc")]
    public DateTime? TimeStampUtc
    {
        get;
        set => Set(ref field, value, () => Latency = DateTime.UtcNow - TimeStampUtc);
    }

    public TimeSpan? Latency
    {
        get;
        private set => Set(ref field, value);
    }

    /// <summary>
    ///     Used to tune PV surplus charging. See also <see cref="OhmPilotTemperatureLimitCelsius" />
    /// </summary>
    [WattPilot("fam", false)]
    public int? PvSurplusBatteryLevelStartCharge
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("pdte", false)]
    public bool? AllowChargingFromBattery
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(CanModifyChargingFromBatteryOptions)));
    }

    public bool CanModifyChargingFromBatteryOptions => AllowChargingFromBattery.HasValue && AllowChargingFromBattery.Value && PvSurplusEnabled.HasValue && PvSurplusEnabled.Value;

    [WattPilot("pdt", false)]
    public byte? PvSurplusBatteryLevelStopCharge
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("pdle", false)]
    public bool? RestrictChargingFromBattery
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("pdls", false)]
    public int? AllowChargingFromBatteryStartSeconds
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(AllowChargingFromBatteryStartString)));
    }

    [WattPilot("pdlo", false)]
    public int? AllowChargingFromBatteryStopSeconds
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(AllowChargingFromBatteryStopString)));
    }


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


    [WattPilot("ebe", false)]
    public bool? EnableBatteryBoost
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("ebo", false)]
    public bool? EnableSingleTimeBoost
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("ebt", false)]
    public byte? MinimumSocInBoost
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("rst", false)]
    public bool? Reboot
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Used to tune PV surplus charging. See also <see cref="PvSurplusBatteryLevelStartCharge" />
    /// </summary>
    [WattPilot("fot", false)]
    public int? OhmPilotTemperatureLimitCelsius
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(OhmPilotTemperatureLimitFahrenheit)));
    }

    public double? OhmPilotTemperatureLimitFahrenheit
    {
        get => OhmPilotTemperatureLimitCelsius * 1.8 + 32;
        set => OhmPilotTemperatureLimitCelsius = value.HasValue ? (int)Math.Round((value.Value - 32) / 1.8, MidpointRounding.AwayFromZero) : null;
    }

    [WattPilot("fup", false)]
    public bool? PvSurplusEnabled
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(CanModifyChargingFromBatteryOptions)));
    }

    [WattPilot("ccw", typeof(WattPilotWifiInfo))]
    public WattPilotWifiInfo? CurrentWifi
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("ful", false)]
    public bool? AwattarEnabled
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("ust", false)]
    public CableLockBehavior? CableLockBehavior
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(CableLockBehaviorDisplayName)));
    }

    [WattPilot("nmo", false)]
    public bool? DisableProtectiveEarth
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("loe", false)]
    public bool? LoadBalancingEnabled
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("ule", false)]
    public bool? EnableOutOfBalanceControl
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("ulu", false)]
    public bool? ShowOutOfBalanceControlInVoltAmpere
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("ula", false)]
    public byte? MaximumOutOfBalanceCurrent
    {
        get;
        set => Set(ref field, value);
    }

    private WattPilotLoadBalancingCurrents? loadBalancingCurrents;

    [WattPilot("lot", false, typeof(WattPilotLoadBalancingCurrents))]
    public WattPilotLoadBalancingCurrents? LoadBalancingCurrents
    {
        get => loadBalancingCurrents;
        set => Set(ref loadBalancingCurrents, value);
    }

    [WattPilot("lof", false)]
    public int? LoadBalancingFallbackCurrent
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("lop", false)]
    public LoadBalancingPriority LoadBalancingPriority
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("rdre", false)]
    public int? RandomDelayPowerFailure
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("rdbf", false)]
    public int? RandomDelayAwattarStart
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("rdef", false)]
    public int? RandomDelayAwattarStop
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("rdbs", false)]
    public int? RandomDelayTimerStart
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("rdes", false)]
    public int? RandomDelayTimerStop
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("rdpl", false)]
    public int? RandomDelayCarConnect
    {
        get;
        set => Set(ref field, value);
    }

    public string? CableLockBehaviorDisplayName => CableLockBehavior?.ToDisplayName();

    public string DisplayName => $"{DeviceName ?? HostName ?? SerialNumber ?? Resources.Unknown}";

    public override string ToString() => DisplayName;

    public object Clone()
    {
        var result = (WattPilot)MemberwiseClone();
        result.IsUpdating = false;

        if (Cards != null)
        {
            IList<WattPilotCard> newCards = new WattPilotCard[Cards.Count];

            for (var i = 0; i < Cards.Count; i++)
            {
                newCards[i] = new WattPilotCard { Energy = Cards[i].Energy, HaveCardId = Cards[i].HaveCardId, Name = Cards[i].Name, };
            }

            result.cards = newCards;
        }

        if (LoadBalancingCurrents is not null)
        {
            result.loadBalancingCurrents = LoadBalancingCurrents.Copy();
        }

        return result;
    }

    public static WattPilot Parse(JToken token)
    {
        return IoC.Get<IGen24JsonService>().ReadFroniusData<WattPilot>(token);
    }
}
