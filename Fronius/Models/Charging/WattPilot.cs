namespace De.Hochstaetter.Fronius.Models.Charging;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class WattPilot : BindableBase, IHaveDisplayName, ICloneable
{
    private string? serialNumber;

    [FroniusProprietaryImport("serial", FroniusDataType.Root)]
    [WattPilot("sse")]
    public string? SerialNumber
    {
        get => serialNumber;
        set => Set(ref serialNumber, value);
    }

    private string? hostName;

    [FroniusProprietaryImport("hostname", FroniusDataType.Root)]
    [WattPilot("ffna")]
    public string? HostName
    {
        get => hostName;
        set => Set(ref hostName, value);
    }

    private AccessMode? accessMode;

    [WattPilot("acs", false)]
    public AccessMode? AccessMode
    {
        get => accessMode;
        set => Set(ref accessMode, value);
    }

    private ChargingLogic? chargingLogic;
    [WattPilot("lmo", false)]
    public ChargingLogic? ChargingLogic
    {
        get => chargingLogic;
        set => Set(ref chargingLogic, value);
    }

    private string? deviceName;

    [WattPilot("fna", false)]
    [FroniusProprietaryImport("friendly_name", FroniusDataType.Root)]
    public string? DeviceName
    {
        get => deviceName;
        set => Set(ref deviceName, value);
    }

    private string? manufacturer;

    [WattPilot("oem")]
    [FroniusProprietaryImport("manufacturer", FroniusDataType.Root)]
    public string? Manufacturer
    {
        get => manufacturer;
        set => Set(ref manufacturer, value);
    }

    private string? model;

    [WattPilot("typ")]
    [FroniusProprietaryImport("devicetype", FroniusDataType.Root)]
    public string? Model
    {
        get => model;
        set => Set(ref model, value);
    }

    private Version? version;

    [FroniusProprietaryImport("version", FroniusDataType.Root)]
    [WattPilot("fwv")]
    public Version? Version
    {
        get => version;
        set => Set(ref version, value);
    }

    private int? protocol;

    [FroniusProprietaryImport("protocol", FroniusDataType.Root)]
    public int? Protocol
    {
        get => protocol;
        set => Set(ref protocol, value);
    }

    private bool? isSecured;

    [FroniusProprietaryImport("secured", FroniusDataType.Root)]
    public bool? IsSecured
    {
        get => isSecured;
        set => Set(ref isSecured, value);
    }

    private double? voltageL1;

    [WattPilot("nrg", 0)]
    public double? VoltageL1
    {
        get => voltageL1;
        set => Set(ref voltageL1, value, () => NotifyOfPropertyChange(nameof(VoltageAverage)));
    }

    private double? voltageL2;

    [WattPilot("nrg", 1)]
    public double? VoltageL2
    {
        get => voltageL2;
        set => Set(ref voltageL2, value, () => NotifyOfPropertyChange(nameof(VoltageAverage)));
    }

    private double? voltageL3;

    [WattPilot("nrg", 2)]
    public double? VoltageL3
    {
        get => voltageL3;
        set => Set(ref voltageL3, value, () => NotifyOfPropertyChange(nameof(VoltageAverage)));
    }

    private double? voltageN;

    [WattPilot("nrg", 3)]
    public double? VoltageN
    {
        get => voltageN;
        set => Set(ref voltageN, value);
    }

    private double? currentL1;

    [WattPilot("nrg", 4)]
    public double? CurrentL1
    {
        get => currentL1;
        set => Set(ref currentL1, value, () => NotifyOfPropertyChange(nameof(CurrentSum)));
    }

    private double? currentL2;

    [WattPilot("nrg", 5)]
    public double? CurrentL2
    {
        get => currentL2;
        set => Set(ref currentL2, value, () => NotifyOfPropertyChange(nameof(CurrentSum)));
    }

    private double? currentL3;

    [WattPilot("nrg", 6)]
    public double? CurrentL3
    {
        get => currentL3;
        set => Set(ref currentL3, value, () => NotifyOfPropertyChange(nameof(CurrentSum)));
    }

    public double? CurrentSum => CurrentL1 + CurrentL2 + CurrentL3;

    private double? powerL1;

    [WattPilot("nrg", 7)]
    public double? PowerL1
    {
        get => powerL1;
        set => Set(ref powerL1, value);
    }

    private double? powerL2;

    [WattPilot("nrg", 8)]
    public double? PowerL2
    {
        get => powerL2;
        set => Set(ref powerL2, value);
    }

    private double? powerL3;

    [WattPilot("nrg", 9)]
    public double? PowerL3
    {
        get => powerL3;
        set => Set(ref powerL3, value);
    }

    private double? powerN;

    [WattPilot("nrg", 10)]
    public double? PowerN
    {
        get => powerN;
        set => Set(ref powerN, value);
    }

    public double? VoltageAverage => (VoltageL1 + VoltageL2 + VoltageL3) / 3;

    private double? powerTotal;

    [WattPilot("nrg", 11)]
    public double? PowerTotal
    {
        get => powerTotal;
        set => Set(ref powerTotal, value);
    }

    private double? powerFactorL1;

    [WattPilot("nrg", 12)]
    public double? PowerFactorL1
    {
        get => powerFactorL1;
        set => Set(ref powerFactorL1, value);
    }

    private double? powerFactorL2;

    [WattPilot("nrg", 13)]
    public double? PowerFactorL2
    {
        get => powerFactorL2;
        set => Set(ref powerFactorL2, value);
    }

    private double? powerFactorL3;

    [WattPilot("nrg", 14)]
    public double? PowerFactorL3
    {
        get => powerFactorL3;
        set => Set(ref powerFactorL3, value);
    }

    private double? powerFactorN;

    [WattPilot("nrg", 15)]
    public double? PowerFactorN
    {
        get => powerFactorN;
        set => Set(ref powerFactorN, value);
    }

    private ModelStatus? status;

    [WattPilot("modelStatus")]
    public ModelStatus? Status
    {
        get => status;
        set => Set(ref status, value, () => NotifyOfPropertyChange(nameof(StatusDisplayName)));
    }

    public string? StatusDisplayName => Status?.ToDisplayName();

    private ModelStatus? statusInternal;

    [WattPilot("msi")]
    public ModelStatus? StatusInternal
    {
        get => statusInternal;
        set => Set(ref statusInternal, value, () => NotifyOfPropertyChange(nameof(StatusInternalDisplayName)));
    }

    public string? StatusInternalDisplayName => StatusInternal?.ToDisplayName();

    private byte? minimumChargingCurrent;

    /// <summary>
    ///     Normally 6A. Can be changed to a higher value if your car can not handle 6A
    /// </summary>
    [WattPilot("mca", false)]
    public byte? MinimumChargingCurrent
    {
        get => minimumChargingCurrent;
        set => Set(ref minimumChargingCurrent, value);
    }

    private int? minimumChargingInterval;

    /// <summary>
    ///     Charge the car at least every amount of milliseconds. Use this if you car disconnects after it has not been charged
    ///     for a while. Set to 0 to disable
    /// </summary>
    [WattPilot("mci", false)]
    public int? MinimumChargingInterval
    {
        get => minimumChargingInterval;
        set => Set(ref minimumChargingInterval, value);
    }

    private int? minimumPauseDuration;
    /// <summary>
    ///     Charging pause duration in milliseconds. Some cars need a minimum pause duration before charging can continue. Set to 0 to disable.
    /// </summary>
    [WattPilot("mcpd", false)]
    public int? MinimumPauseDuration
    {
        get => minimumPauseDuration;
        set => Set(ref minimumPauseDuration, value);
    }

    private bool? allowChargingPause;
    [WattPilot("fap", false)] //go-eCharger uses "acp" for this
    public bool? AllowChargingPause
    {
        get => allowChargingPause;
        set => Set(ref allowChargingPause, value);
    }

    private AwattarCountry energyPriceCountry;

    /// <summary>
    ///     Used to get the correct market price for energy
    /// </summary>
    [WattPilot("awc", false)]
    public AwattarCountry EnergyPriceCountry
    {
        get => energyPriceCountry;
        set => Set(ref energyPriceCountry, value);
    }

    private float? maxEnergyPrice;

    /// <summary>
    ///     Maximum energy price to allow charging in ct/kWh
    /// </summary>
    [WattPilot("awp", false)]
    public float? MaxEnergyPrice
    {
        get => maxEnergyPrice;
        set => Set(ref maxEnergyPrice, value);
    }

    private bool? simulateUnplugging;
    /// <summary>
    /// Some cars need this. If yours does not, leave it to false
    /// </summary>
    [WattPilot("su", false)]
    public bool? SimulateUnplugging
    {
        get => simulateUnplugging;
        set => Set(ref simulateUnplugging, value);
    }

    private bool? simulateUnpluggingAlways;
    /// <summary>
    /// Unclear, what this is good for. The app only uses <see cref="SimulateUnplugging"/>
    /// </summary>
    [WattPilot("sua", false)]
    public bool? SimulateUnpluggingAlways
    {
        get => simulateUnpluggingAlways;
        set => Set(ref simulateUnpluggingAlways, value);
    }

    private int? minimumTimeBetweenPhaseSwitches;

    /// <summary>
    ///     Minimum time before a phase switch occurs in milliseconds
    /// </summary>
    [WattPilot("mptwt", false)]
    public int? MinimumTimeBetweenPhaseSwitches
    {
        get => minimumTimeBetweenPhaseSwitches;
        set => Set(ref minimumTimeBetweenPhaseSwitches, value);
    }

    private int? phaseSwitchTriggerTime;

    /// <summary>
    ///     Minimum time in milliseconds the PV surplus must be above or below <see cref="PhaseSwitchPower" /> before the phase
    ///     switch is requested
    /// </summary>
    [WattPilot("mpwst", false)]
    public int? PhaseSwitchTriggerTime
    {
        get => phaseSwitchTriggerTime;
        set => Set(ref phaseSwitchTriggerTime, value);
    }

    private double? phaseSwitchPower;

    /// <summary>
    ///     If PV surplus is above that power, switch to 3 phases. If PV surplus is below, switch to 1 phase.
    ///     Use <see cref="PhaseSwitchTriggerTime" /> to control the minimum time before the phase switch occurs.
    /// </summary>
    [WattPilot("spl3", false)]
    public double? PhaseSwitchPower
    {
        get => phaseSwitchPower;
        set => Set(ref phaseSwitchPower, value);
    }

    private byte? absoluteMaximumChargingCurrent;

    /// <summary>
    ///     Set this accordingly to comply with your DNO, e.g. don´t use 32 A if your house only supports 35A
    /// </summary>
    [WattPilot("ama", false)]
    public byte? AbsoluteMaximumChargingCurrent
    {
        get => absoluteMaximumChargingCurrent;
        set => Set(ref absoluteMaximumChargingCurrent, value);
    }

    private bool? unlockCableOnPowerFailure;

    /// <summary>
    ///     Gets or sets whether you want the cable to unlock if the WattPilot is not powered.
    /// </summary>
    [WattPilot("upo", false)]
    public bool? UnlockCableOnPowerFailure
    {
        get => unlockCableOnPowerFailure;
        set => Set(ref unlockCableOnPowerFailure, value);
    }

    private EcoRoundingMode roundingMode;
    /// <summary>
    /// If you don´t have a house battery, you can specify whether you prefer power from/to grid. If you have a battery, this setting does not make a big difference
    /// </summary>
    [WattPilot("frm", false)]
    public EcoRoundingMode RoundingMode
    {
        get => roundingMode;
        set => Set(ref roundingMode, value);
    }

    private float? pvSurplusPowerThreshold;
    /// <summary>
    /// Minimum power in Watts to start PV surplus charging
    /// </summary>
    [WattPilot("fst", false)]
    public float? PvSurplusPowerThreshold
    {
        get => pvSurplusPowerThreshold;
        set => Set(ref pvSurplusPowerThreshold, value);
    }

    private int? minimumChargingTime;
    /// <summary>
    /// Once charging has started, it continues for at least <see cref="MinimumChargingTime"/> milliseconds.
    /// </summary>
    [WattPilot("fmt", false)]
    public int? MinimumChargingTime
    {
        get => minimumChargingTime;
        set => Set(ref minimumChargingTime, value);
    }

    private byte? maximumChargingCurrent;

    /// <summary>
    ///     The current active charging limit. Must be between <see cref="MinimumChargingCurrent" /> and
    ///     <see cref="AbsoluteMaximumChargingCurrent" />
    /// </summary>
    [WattPilot("amp", false)]
    public byte? MaximumChargingCurrent
    {
        get => maximumChargingCurrent;
        set => Set(ref maximumChargingCurrent, value);
    }

    private string? downloadLink;

    [WattPilot("dll")]
    public string? DownloadLink
    {
        get => downloadLink;
        set => Set(ref downloadLink, value);
    }

    private string? cloudAccessKey;

    /// <summary>
    ///     Token to be used at https://&lt;serial number&gt;.api.v3.go-e.io/
    ///     Requires that <see cref="CloudAccessEnabled" /> is true
    /// </summary>
    [WattPilot("cak", false)]
    public string? CloudAccessKey
    {
        get => cloudAccessKey;
        set => Set(ref cloudAccessKey, value);
    }

    private bool? cloudAccessEnabled;

    /// <summary>
    ///     True: Can use go-E charger Cloud Api
    /// </summary>
    [WattPilot("cae", false)]
    public bool? CloudAccessEnabled
    {
        get => cloudAccessEnabled;
        set => Set(ref cloudAccessEnabled, value);
    }

    private CarStatus? carStatus;

    [WattPilot("car")]
    public CarStatus? CarStatus
    {
        get => carStatus;
        set => Set(ref carStatus, value);
    }

    private double? allowedCurrent;

    /// <summary>
    ///     Read-Only: is null when no car is connected
    /// </summary>
    [WattPilot("acu")]
    public double? AllowedCurrent
    {
        get => allowedCurrent;
        set => Set(ref allowedCurrent, value);
    }

    private double? allowedCurrentInternal;

    [WattPilot("acui")]
    public double? AllowedCurrentInternal
    {
        get => allowedCurrentInternal;
        set => Set(ref allowedCurrentInternal, value);
    }

    private bool? isChargingAllowed;

    /// <summary>
    ///     Read-only: All prerequisites met (RFID auth, <see cref="EnableCharging" />, etc.)
    /// </summary>
    [WattPilot("alw")]
    public bool? IsChargingAllowed
    {
        get => isChargingAllowed;
        set => Set(ref isChargingAllowed, value);
    }

    private bool? enableCharging;

    /// <summary>
    ///     Read-write: Allow/Disallow charging
    /// </summary>
    [WattPilot("bac", false)]
    public bool? EnableCharging
    {
        get => enableCharging;
        set => Set(ref enableCharging, value);
    }

    private double? totalEnergy;

    /// <summary>
    ///     Total energy delivered by the charger. Includes energy of current charging session
    /// </summary>
    [WattPilot("eto")]
    public double? TotalEnergy
    {
        get => totalEnergy;
        set => Set(ref totalEnergy, value);
    }

    private double? totalEnergyWithoutCurrentSession;

    /// <summary>
    ///     Total energy delivered by the charger. Does not include energy of current charging session
    /// </summary>
    [WattPilot("etop")]
    public double? TotalEnergyWithoutCurrentSession
    {
        get => totalEnergyWithoutCurrentSession;
        set => Set(ref totalEnergyWithoutCurrentSession, value);
    }

    private double? cableCurrentMaximum;

    /// <summary>
    ///     Maximum current that the connected cable supports.
    /// </summary>
    [WattPilot("cbl")]
    public double? CableCurrentMaximum
    {
        get => cableCurrentMaximum;
        set => Set(ref cableCurrentMaximum, value);
    }

    private byte? maximumWattPilotPowerKiloWatts;

    /// <summary>
    ///     Maximum Power that the WattPilot is able to deliver (11 or 22 kW)
    /// </summary>
    [WattPilot("var")]
    public byte? MaximumWattPilotPowerKiloWatts
    {
        get => maximumWattPilotPowerKiloWatts;
        set => Set(ref maximumWattPilotPowerKiloWatts, value,()=>NotifyOfPropertyChange(nameof(MaximumWattPilotPower)));
    }

    public double? MaximumWattPilotPower => MaximumWattPilotPowerKiloWatts * 1000d;

    private PhaseSwitchMode? phaseSwitchMode;

    [WattPilot("psm", false)]
    public PhaseSwitchMode? PhaseSwitchMode
    {
        get => phaseSwitchMode;
        set => Set(ref phaseSwitchMode, value);
    }

    private bool? noFeedIn;
    /// <summary>
    /// If your inverter does not feed the grid, enable this
    /// </summary>
    [WattPilot("fzf", false)]
    public bool? NoFeedIn
    {
        get => noFeedIn;
        set => Set(ref noFeedIn, value);
    }

    private IList<WattPilotCard>? cards;

    [WattPilot("cards")]
    public IList<WattPilotCard>? Cards
    {
        get => cards;
        set => Set(ref cards, value);
    }

    private bool? is16AmpereVariant;

    /// <summary>
    ///     Is this the 16 Amperes limited version ("Go 11 J" or "Home 11 J")?
    /// </summary>
    [WattPilot("adi")]
    public bool? Is16AmpereVariant
    {
        get => is16AmpereVariant;
        set => Set(ref is16AmpereVariant, value, () => NotifyOfPropertyChange(nameof(MaxWattPilotCurrent)));
    }

    /// <summary>
    ///     Maximum current that the WattPilot can deliver
    /// </summary>
    public byte? MaxWattPilotCurrent => !Is16AmpereVariant.HasValue ? null : Is16AmpereVariant.Value ? (byte)16 : (byte)32;

    private double? frequency;

    [WattPilot("fhz")]
    public double? Frequency
    {
        get => frequency;
        set => Set(ref frequency, value);
    }

    private DateTime? timeStampUtc;

    [WattPilot("utc")]
    public DateTime? TimeStampUtc
    {
        get => timeStampUtc;
        set => Set(ref timeStampUtc, value);
    }

    private int? pvSurplusBatteryLevel;

    /// <summary>
    ///     Used to tune PV surplus charging. See also <see cref="OhmPilotTemperatureLimitCelsius" />
    /// </summary>
    [WattPilot("fam", false)]
    public int? PvSurplusBatteryLevel
    {
        get => pvSurplusBatteryLevel;
        set => Set(ref pvSurplusBatteryLevel, value);
    }

    private bool? reboot;
    [WattPilot("rst", false)]
    public bool? Reboot
    {
        get => reboot;
        set => Set(ref reboot, value);
    }

    private int? ohmPilotTemperatureLimitCelsius;

    /// <summary>
    ///     Used to tune PV surplus charging. See also <see cref="PvSurplusBatteryLevel" />
    /// </summary>
    [WattPilot("fot", false)]
    public int? OhmPilotTemperatureLimitCelsius
    {
        get => ohmPilotTemperatureLimitCelsius;
        set => Set(ref ohmPilotTemperatureLimitCelsius, value, () => NotifyOfPropertyChange(nameof(OhmPilotTemperatureLimitFahrenheit)));
    }

    public double? OhmPilotTemperatureLimitFahrenheit
    {
        get => OhmPilotTemperatureLimitCelsius * 1.8 + 32;
        set => OhmPilotTemperatureLimitCelsius = value.HasValue ? (int)Math.Round((value.Value - 32) / 1.8, MidpointRounding.AwayFromZero) : null;
    }

    private bool? pvSurplusEnabled;

    [WattPilot("fup", false)]
    public bool? PvSurplusEnabled
    {
        get => pvSurplusEnabled;
        set => Set(ref pvSurplusEnabled, value);
    }

    private bool? awattarEnabled;
    [WattPilot("ful", false)]
    public bool? AwattarEnabled
    {
        get => awattarEnabled;
        set => Set(ref awattarEnabled, value);
    }

    private CableLockBehavior? cableLockBehavior;
    [WattPilot("ust", false)]
    public CableLockBehavior? CableLockBehavior
    {
        get => cableLockBehavior;
        set => Set(ref cableLockBehavior, value, () => NotifyOfPropertyChange(nameof(CableLockBehaviorDisplayName)));
    }

    private bool? disableProtectiveEarth;
    [WattPilot("nmo", false)]
    public bool? DisableProtectiveEarth
    {
        get => disableProtectiveEarth;
        set => Set(ref disableProtectiveEarth, value);
    }

    public string? CableLockBehaviorDisplayName => CableLockBehavior?.ToDisplayName();

    public string DisplayName => $"{DeviceName ?? HostName ?? SerialNumber ?? Resources.Unknown}";

    public override string ToString() => DisplayName;

    public object Clone()
    {
        var result = (WattPilot)MemberwiseClone();

        if (Cards != null)
        {
            IList<WattPilotCard> newCards = new WattPilotCard[Cards.Count];

            for (var i = 0; i < Cards.Count; i++)
            {
                newCards[i] = new WattPilotCard { Energy = Cards[i].Energy, HaveCardId = Cards[i].HaveCardId, Name = Cards[i].Name, };
            }

            result.Cards = newCards;
        }

        return result;
    }

    public static WattPilot Parse(JToken token)
    {
        return IoC.Get<IGen24JsonService>().ReadFroniusData<WattPilot>(token);
    }
}
