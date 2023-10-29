namespace De.Hochstaetter.Fronius.Models.Modbus
{
    public class SunSpecInverterExtendedMeasurements : SunSpecModelBase
    {
        public SunSpecInverterExtendedMeasurements(ReadOnlyMemory<byte> data, ushort modelNumber, ushort absoluteRegister) : base(data, modelNumber, absoluteRegister) { }

        public override IReadOnlyList<ushort> SupportedModels { get; } = new ushort[] { 122 };

        public override ushort MinimumDataLength => 44;

        [Modbus(0)]
        public SunSpecConnectionStatus PvConnectionStatus
        {
            get => Get<SunSpecConnectionStatus>();
            set => Set(value);
        }

        [Modbus(1)]
        public SunSpecConnectionStatus StorageConnectionStatus
        {
            get => Get<SunSpecConnectionStatus>();
            set => Set(value);
        }

        [Modbus(2)]
        public SunSpecConnectionStatus GridConnectionStatus
        {
            get => Get<SunSpecConnectionStatus>();
            set => Set(value);
        }

        [Modbus(3)]
        public ulong EnergyActiveLifeTimeI
        {
            get => Get<ulong>();
            set => Set(value);
        }

        public double? EnergyActiveLifeTime
        {
            get => ToDouble(EnergyActiveLifeTimeI, 0, true);
            set => EnergyActiveLifeTimeI = FromDouble<ulong>(value, 0, true);
        }

        [Modbus(7)]
        public ulong EnergyApparentLifeTimeI
        {
            get => Get<ulong>();
            set => Set(value);
        }

        public double? EnergyApparentLifeTime
        {
            get => ToDouble(EnergyApparentLifeTimeI, 0, true);
            set => EnergyApparentLifeTimeI = FromDouble<ulong>(value, 0, true);
        }

        [Modbus(11)]
        public ulong EnergyReactiveQ1LifeTimeI
        {
            get => Get<ulong>();
            set => Set(value);
        }

        public double? EnergyReactiveQ1LifeTime
        {
            get => ToDouble(EnergyReactiveQ1LifeTimeI, 0, true);
            set => EnergyReactiveQ1LifeTimeI = FromDouble<ulong>(value, 0, true);
        }

        [Modbus(15)]
        public ulong EnergyReactiveQ2LifeTimeI
        {
            get => Get<ulong>();
            set => Set(value);
        }

        public double? EnergyReactiveQ2LifeTime
        {
            get => ToDouble(EnergyReactiveQ2LifeTimeI, 0, true);
            set => EnergyReactiveQ2LifeTimeI = FromDouble<ulong>(value, 0, true);
        }

        [Modbus(19)]
        public ulong EnergyReactiveQ3LifeTimeI
        {
            get => Get<ulong>();
            set => Set(value);
        }

        public double? EnergyReactiveQ3LifeTime
        {
            get => ToDouble(EnergyReactiveQ3LifeTimeI, 0, true);
            set => EnergyReactiveQ3LifeTimeI = FromDouble<ulong>(value, 0, true);
        }

        [Modbus(23)]
        public ulong EnergyReactiveQ4LifeTimeI
        {
            get => Get<ulong>();
            set => Set(value);
        }

        public double? EnergyReactiveQ4LifeTime
        {
            get => ToDouble(EnergyReactiveQ4LifeTimeI, 0, true);
            set => EnergyReactiveQ4LifeTimeI = FromDouble<ulong>(value, 0, true);
        }

        [Modbus(27)]
        public short ReactivePowerAvailableI
        {
            get => Get<short>();
            set => Set(value);
        }

        [Modbus(28)]
        public short ReactivePowerAvailableSf
        {
            get => Get<short>();
            set => Set(value);
        }

        public double? ReactivePowerAvailable
        {
            get => ToDouble(ReactivePowerAvailableI, ReactivePowerAvailableSf);
            set => ReactivePowerAvailableI = FromDouble<short>(value, ReactivePowerAvailableSf);
        }

        [Modbus(29)]
        public short ActivePowerAvailableI
        {
            get => Get<short>();
            set => Set(value);
        }

        [Modbus(30)]
        public short ActivePowerAvailableSf
        {
            get => Get<short>();
            set => Set(value);
        }

        public double? ActivePowerAvailable
        {
            get => ToDouble(ActivePowerAvailableI, ActivePowerAvailableSf);
            set => ActivePowerAvailableI = FromDouble<short>(value, ActivePowerAvailableSf);
        }

        [Modbus(31)]
        public SunSpecLimitsReached LimitsReached
        {
            get => Get<SunSpecLimitsReached>();
            set => Set(value);
        }

        [Modbus(33)]
        public SunSpecLimitControlsActive LimitControlsActive
        {
            get => Get<SunSpecLimitControlsActive>();
            set => Set(value);
        }

        [Modbus(35, 4)]
        public string? TimeSourceName
        {
            get => GetString();
            set => SetString(value);
        }

        [Modbus(39)]
        public uint TimeStampI
        {
            get => Get<uint>();
            set => Set(value);
        }

        public DateTime? TimeStamp
        {
            get => ToDateTime(TimeStampI);
            set => TimeStampI = ToSunSpecTime(value);
        }

        [Modbus(41)]
        public SunSpecVoltageRideThroughModesAvailable VoltageRideThroughModesAvailable
        {
            get => Get<SunSpecVoltageRideThroughModesAvailable>();
            set => Set(value);
        }

        [Modbus(42)]
        public ushort IsolationResistanceI
        {
            get => Get<ushort>();
            set => Set(value);
        }

        [Modbus(43)]
        public short IsolationResistanceSf
        {
            get => Get<short>();
            set => Set(value);
        }

        public double? IsolationResistance
        {
            get => ToDouble(IsolationResistanceI, IsolationResistanceSf);
            set => IsolationResistanceI = FromDouble<ushort>(value, IsolationResistanceSf);
        }
    }
}
