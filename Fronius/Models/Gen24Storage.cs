using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using De.Hochstaetter.Fronius.Attributes;

namespace De.Hochstaetter.Fronius.Models
{
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public class Gen24Storage:Gen24DeviceBase
    {
        private double? current;

        [Fronius("BAT_CURRENT_DC_F64")]
        public double? Current
        {
            get => current;
            set => Set(ref current, value,()=>NotifyOfPropertyChange(nameof(Power)));
        }

        private double? currentInternal;

        [Fronius("BAT_CURRENT_DC_INTERNAL_F64")]
        public double? CurrentInternal
        {
            get => currentInternal;
            set => Set(ref currentInternal, value, () => NotifyOfPropertyChange(nameof(PowerInternal)));
        }

        private double? availableCapacity;
        [Fronius("BAT_ENERGYACTIVE_ESTIMATION_MAX_CAPACITY_F64", Unit.Joule)]
        public double? AvailableCapacity
        {
            get => availableCapacity;
            set => Set(ref availableCapacity, value);
        }

        private double? lifeTimeCharged;
        [Fronius("BAT_ENERGYACTIVE_LIFETIME_CHARGED_F64", Unit.Joule)]
        public double? LifeTimeCharged
        {
            get => lifeTimeCharged;
            set => Set(ref lifeTimeCharged, value, () => NotifyOfPropertyChange(nameof(Efficiency)));
        }

        private double? lifeTimeDischarged;
        [Fronius("BAT_ENERGYACTIVE_LIFETIME_DISCHARGED_F64", Unit.Joule)]
        public double? LifeTimeDischarged
        {
            get => lifeTimeDischarged;
            set => Set(ref lifeTimeDischarged, value,()=>NotifyOfPropertyChange(nameof(Efficiency)));
        }

        private double? maxCapacity;
        [Fronius("BAT_ENERGYACTIVE_MAX_CAPACITY_F64", Unit.Joule)]
        public double? MaxCapacity
        {
            get => maxCapacity;
            set => Set(ref maxCapacity, value);
        }

        private ushort? cellState;
        [Fronius("BAT_MODE_CELL_STATE_U16")]
        public ushort? CellState
        {
            get => cellState;
            set => Set(ref cellState, value);
        }

        private double? cellTemperatureAverage;

        [Fronius("BAT_TEMPERATURE_CELL_F64")]
        public double? CellTemperatureAverage
        {
            get => cellTemperatureAverage;
            set => Set(ref cellTemperatureAverage, value);
        }

        private double? cellTemperatureMax;

        [Fronius("BAT_TEMPERATURE_CELL_MAX_F64")]
        public double? CellTemperatureMax
        {
            get => cellTemperatureMax;
            set => Set(ref cellTemperatureMax, value);
        }

        private double? cellTemperatureMin;

        [Fronius("BAT_TEMPERATURE_CELL_MIN_F64")]
        public double? CellTemperatureMin
        {
            get => cellTemperatureMin;
            set => Set(ref cellTemperatureMin, value);
        }

        private double? stateOfCharge;
        [Fronius("BAT_VALUE_STATE_OF_CHARGE_RELATIVE_U16", Unit.Percent)]
        public double? StateOfCharge
        {
            get => stateOfCharge;
            set => Set(ref stateOfCharge, value);
        }


        private double? stateOfHealth;
        [Fronius("BAT_VALUE_STATE_OF_HEALTH_RELATIVE_U16", Unit.Percent)]
        public double? StateOfHealth
        {
            get => stateOfHealth;
            set => Set(ref stateOfHealth, value);
        }

        private ushort? warningCode;
        [Fronius("BAT_VALUE_WARNING_CODE_U16")]
        public ushort? WarningCode
        {
            get => warningCode;
            set => Set(ref warningCode, value);
        }

        private double? voltageInternal;
        [Fronius("BAT_VOLTAGE_DC_INTERNAL_F64")]
        public double? VoltageInternal
        {
            get => voltageInternal;
            set => Set(ref voltageInternal, value, () => NotifyOfPropertyChange(nameof(PowerInternal)));
        }

        private double? disChargeLimit;
        [Fronius("DCLINK_POWERACTIVE_LIMIT_DISCHARGE_F64")]
        public double? DisChargeLimit
        {
            get => disChargeLimit;
            set => Set(ref disChargeLimit, value);
        }

        private double? maxPower;
        [Fronius("DCLINK_POWERACTIVE_MAX_F32")]
        public double? MaxPower
        {
            get => maxPower;
            set => Set(ref maxPower, value);
        }

        private double? voltage;
        [Fronius("DCLINK_VOLTAGE_MEAN_F32")]
        public double? Voltage
        {
            get => voltage;
            set => Set(ref voltage, value, () => NotifyOfPropertyChange(nameof(Power)));
        }

        private double ambientTemperature;
        [Fronius("DEVICE_TEMPERATURE_AMBIENTEMEAN_F32")]
        public double AmbientTemperature
        {
            get => ambientTemperature;
            set => Set(ref ambientTemperature, value);
        }

        private ushort? hybridOperatingState;
        [Fronius("BAT_MODE_HYBRID_OPERATING_STATE_U16")]
        public ushort? HybridOperatingState
        {
            get => hybridOperatingState;
            set => Set(ref hybridOperatingState, value);
        }

        private ushort? lastFaultParameter;
        [Fronius("BAT_MODE_LAST_FAULT_PARAMETER_U16")]
        public ushort? LastFaultParameter
        {
            get => lastFaultParameter;
            set => Set(ref lastFaultParameter, value);
        }

        private ushort? state;
        [Fronius("BAT_MODE_STATE_U16")]
        public ushort? State
        {
            get => state;
            set => Set(ref state, value);
        }

        private ushort? mode;
        [Fronius("BAT_MODE_U16")]
        public ushort? Mode
        {
            get => mode;
            set => Set(ref mode, value);
        }

        private bool? isAwake;
        [Fronius("BAT_MODE_WAKE_ENABLE_STATUS_U16")]
        public bool? IsAwake
        {
            get => isAwake;
            set => Set(ref isAwake, value);
        }

        private Version? hardwareVersion;
        [Fronius("hw_version", FroniusDataType.Attribute)]
        public Version? HardwareVersion
        {
            get => hardwareVersion;
            set => Set(ref hardwareVersion, value);
        }

        private Version? softwareVersion;
        [Fronius("sw_version", FroniusDataType.Attribute)]
        public Version? SoftwareVersion
        {
            get => softwareVersion;
            set => Set(ref softwareVersion, value);
        }

        private bool? hasInternalDcDc;
        [Fronius("has_internal_dcdc", FroniusDataType.Attribute)]
        public bool? HasInternalDcDc
        {
            get => hasInternalDcDc;
            set => Set(ref hasInternalDcDc, value);
        }

        private bool? canConfigureInternalDcDc;
        [Fronius("internal_dcdc_is_configurable", FroniusDataType.Attribute)]
        public bool? CanConfigureInternalDcDc
        {
            get => canConfigureInternalDcDc;
            set => Set(ref canConfigureInternalDcDc, value);
        }

        private double maxVoltage;
        [Fronius("max_udc", FroniusDataType.Attribute)]
        public double MaxVoltage
        {
            get => maxVoltage;
            set => Set(ref maxVoltage, value);
        }

        private double minVoltage;
        [Fronius("min_udc", FroniusDataType.Attribute)]
        public double MinVoltage
        {
            get => minVoltage;
            set => Set(ref minVoltage, value);
        }

        public double? Power => Voltage * Current;

        public double? PowerInternal => VoltageInternal * CurrentInternal;

        public double? Efficiency => LifeTimeDischarged / LifeTimeCharged;
    }
}
