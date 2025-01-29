namespace De.Hochstaetter.Fronius.Models.Gen24;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class Gen24Storage : Gen24DeviceBase
{
    [FroniusProprietaryImport("BAT_CURRENT_DC_F64")]
    public double? Current
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(Power)));
    }

    [FroniusProprietaryImport("BAT_CURRENT_DC_INTERNAL_F64")]
    public double? CurrentInternal
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(PowerInternal)));
    }

    [FroniusProprietaryImport("BAT_ENERGYACTIVE_ESTIMATION_MAX_CAPACITY_F64", Unit.Joule)]
    public double? AvailableCapacity
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("BAT_ENERGYACTIVE_LIFETIME_CHARGED_F64", Unit.Joule)]
    public double? LifeTimeCharged
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(Efficiency)));
    }

    [FroniusProprietaryImport("BAT_ENERGYACTIVE_LIFETIME_DISCHARGED_F64", Unit.Joule)]
    public double? LifeTimeDischarged
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(Efficiency)));
    }

    [FroniusProprietaryImport("BAT_ENERGYACTIVE_MAX_CAPACITY_F64", Unit.Joule)]
    public double? MaxCapacity
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("BAT_MODE_CELL_STATE_U16")]
    public ushort? CellState
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("BAT_TEMPERATURE_CELL_F64")]
    public double? CellTemperatureAverage
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("BAT_TEMPERATURE_CELL_MAX_F64")]
    public double? CellTemperatureMax
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("BAT_TEMPERATURE_CELL_MIN_F64")]
    public double? CellTemperatureMin
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("BAT_VALUE_STATE_OF_CHARGE_RELATIVE_U16", Unit.Percent)]
    public double? StateOfCharge
    {
        get;
        set => Set(ref field, value);
    }

    public double MinimumStateOfCharge
    {
        get;
        set => Set(ref field, value);
    }

    public double MaximumStateOfCharge
    {
        get;
        set => Set(ref field, value);
    }


    [FroniusProprietaryImport("BAT_VALUE_STATE_OF_HEALTH_RELATIVE_U16", Unit.Percent)]
    public double? StateOfHealth
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("BAT_VALUE_WARNING_CODE_U16")]
    public ushort? WarningCode
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("BAT_VOLTAGE_DC_INTERNAL_F64")]
    public double? VoltageInternal
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(PowerInternal)));
    }

    [FroniusProprietaryImport("DCLINK_POWERACTIVE_LIMIT_DISCHARGE_F64")]
    public double? DisChargeLimit
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("DCLINK_POWERACTIVE_MAX_F32")]
    public double? MaxPower
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("DCLINK_VOLTAGE_MEAN_F32")]
    public double? VoltageOuter
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(Power)));
    }

    [FroniusProprietaryImport("DEVICE_TEMPERATURE_AMBIENTEMEAN_F32")]
    public double AmbientTemperature
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("BAT_MODE_HYBRID_OPERATING_STATE_U16")]
    public ushort? HybridOperatingState
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("BAT_MODE_LAST_FAULT_PARAMETER_U16")]
    public ushort? LastFaultParameter
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("BAT_MODE_STATE_U16")]
    public uint? State
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("BAT_MODE_U16")]
    public ushort? Mode
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("BAT_MODE_WAKE_ENABLE_STATUS_U16")]
    public bool? IsAwake
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("hw_version", FroniusDataType.Attribute)]
    public Version? HardwareVersion
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("sw_version", FroniusDataType.Attribute)]
    public Version? SoftwareVersion
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("has_internal_dcdc", FroniusDataType.Attribute)]
    public bool? HasInternalDcDc
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("internal_dcdc_is_configurable", FroniusDataType.Attribute)]
    public bool? CanConfigureInternalDcDc
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("max_udc", FroniusDataType.Attribute)]
    public double MaxVoltage
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("min_udc", FroniusDataType.Attribute)]
    public double MinVoltage
    {
        get;
        set => Set(ref field, value);
    }

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