namespace De.Hochstaetter.Fronius.Models.Gen24;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public partial class Gen24Storage : Gen24DeviceBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Power))]
    [FroniusProprietaryImport("BAT_CURRENT_DC_F64")]
    public partial double? Current { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PowerInternal))]
    [FroniusProprietaryImport("BAT_CURRENT_DC_INTERNAL_F64")]
    public partial double? CurrentInternal { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("BAT_ENERGYACTIVE_ESTIMATION_MAX_CAPACITY_F64", Unit.Joule)]
    public partial double? AvailableCapacity { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Efficiency))]
    [FroniusProprietaryImport("BAT_ENERGYACTIVE_LIFETIME_CHARGED_F64", Unit.Joule)]
    public partial double? LifeTimeCharged { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Efficiency))]
    [FroniusProprietaryImport("BAT_ENERGYACTIVE_LIFETIME_DISCHARGED_F64", Unit.Joule)]
    public partial double? LifeTimeDischarged { get; set; }

    [FroniusProprietaryImport("BAT_ENERGYACTIVE_MAX_CAPACITY_F64", Unit.Joule)]
    [ObservableProperty]
    public partial double? MaxCapacity { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusString),nameof(TrafficLight))]
    [FroniusProprietaryImport("BAT_MODE_CELL_STATE_U16")]
    public partial ushort? CellState { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("BAT_TEMPERATURE_CELL_F64")]
    public partial double? CellTemperatureAverage { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("BAT_TEMPERATURE_CELL_MAX_F64")]
    public partial double? CellTemperatureMax { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("BAT_TEMPERATURE_CELL_MIN_F64")]
    public partial double? CellTemperatureMin { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("BAT_VALUE_STATE_OF_CHARGE_RELATIVE_U16", Unit.Percent)]
    public partial double? StateOfCharge { get; set; }

    [ObservableProperty]
    public partial double MinimumStateOfCharge { get; set; }

    [ObservableProperty]
    public partial double MaximumStateOfCharge { get; set; }


    [ObservableProperty]
    [FroniusProprietaryImport("BAT_VALUE_STATE_OF_HEALTH_RELATIVE_U16", Unit.Percent)]
    public partial double? StateOfHealth { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("BAT_VALUE_WARNING_CODE_U16")]
    public partial ushort? WarningCode { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PowerInternal))]
    [FroniusProprietaryImport("BAT_VOLTAGE_DC_INTERNAL_F64")]
    public partial double? VoltageInternal { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("DCLINK_POWERACTIVE_LIMIT_DISCHARGE_F64")]
    public partial double? DisChargeLimit { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("DCLINK_POWERACTIVE_MAX_F32")]
    public partial double? MaxPower { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Power))]
    [FroniusProprietaryImport("DCLINK_VOLTAGE_MEAN_F32")]
    public partial double? VoltageOuter { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("DEVICE_TEMPERATURE_AMBIENTEMEAN_F32")]
    public partial double AmbientTemperature { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("BAT_MODE_HYBRID_OPERATING_STATE_U16")]
    public partial ushort? HybridOperatingState { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("BAT_MODE_LAST_FAULT_PARAMETER_U16")]
    public partial ushort? LastFaultParameter { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("BAT_MODE_STATE_U16")]
    public partial uint? State { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("BAT_MODE_U16")]
    public partial ushort? Mode { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("BAT_MODE_WAKE_ENABLE_STATUS_U16")]
    public partial bool? IsAwake { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("hw_version", FroniusDataType.Attribute)]
    public partial Version? HardwareVersion { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("sw_version", FroniusDataType.Attribute)]
    public partial Version? SoftwareVersion { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("has_internal_dcdc", FroniusDataType.Attribute)]
    public partial bool? HasInternalDcDc { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("internal_dcdc_is_configurable", FroniusDataType.Attribute)]
    public partial bool? CanConfigureInternalDcDc { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("max_udc", FroniusDataType.Attribute)]
    public partial double? MaxVoltage { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("min_udc", FroniusDataType.Attribute)]
    public partial double? MinVoltage { get; set; }

    public double? Power => VoltageOuter * Current;

    public double? PowerInternal => VoltageInternal * CurrentInternal;

    public double? Efficiency => LifeTimeDischarged / LifeTimeCharged;

    public TrafficLight TrafficLight => Manufacturer switch
    {
        "BYD" => CellState switch
        {
            3 => TrafficLight.Green,
            4 => TrafficLight.Red,
            _ => TrafficLight.Yellow
        },

        "LG-Chem" => CellState switch
        {
            3 => TrafficLight.Green,
            5 => TrafficLight.Red,
            _ => TrafficLight.Yellow,
        },

        _ => TrafficLight.Yellow
    };

    public string StatusString => Manufacturer switch
    {
        "BYD" => CellState switch
        {
            0 => Resources.Standby,
            1 => Resources.Inactive,
            2 => Resources.Starting,
            3 => Resources.Active,
            4 => Resources.Error,
            5 => Resources.Updating,
            _ => Resources.Unknown
        },
        "LG-Chem" => CellState switch
        {
            1 => Resources.Standby,
            3 => Resources.Active,
            5 => Resources.Error,
            10 => Resources.Sleeping,
            _ => Resources.Unknown
        },
        _ => Resources.Unknown
    };

}