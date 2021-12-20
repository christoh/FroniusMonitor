namespace De.Hochstaetter.Fronius.Models
{
    public class ThreePhasesData:BaseResponse
    {
        public double? L1Voltage { get; init; }
        public double? L2Voltage { get; init; }
        public double? L3Voltage { get; init; }
        public double? L1Current { get; init; }
        public double? L2Current { get; init; }
        public double? L3Current { get; init; }
        public double? L1PowerWatts => L1Voltage * L1Current;
        public double? L2PowerWatts => L2Voltage * L2Current;
        public double? L3PowerWatts => L3Voltage * L3Current;
        public double? AcPowerTotalWatts => L1PowerWatts + L2PowerWatts + L3PowerWatts;
        public double? AcPowerTotalKiloWatts => AcPowerTotalWatts / 1000;
    }

    public class InverterData:BaseResponse
    {
        public InverterStatus Status { get; init; }
        public string InverterStatusString => Status.ToDisplayName();
        public double? CurrentString1 { get; init; }
        public double? CurrentString2 { get; init; }
        public double? CurrentStorage { get; init; }
        public double? VoltageString1 { get; init; }
        public double? VoltageString2 { get; init; }
        public double? VoltageStorage { get; init; }
        public double? BatteryPowerWatts => CurrentStorage * VoltageStorage;
        public double? String1PowerWatts => CurrentString1 * VoltageString1;
        public double? String2PowerWatts => CurrentString2 * VoltageString2;
        public double? SolarPowerWatts => (String1PowerWatts ?? 0) + (String2PowerWatts ?? 0);
        public double? TotalEnergyWattHours { get; init; }
        public double? TotalEnergyKiloWattHours => TotalEnergyWattHours / 1000;
        public int ErrorCode { get; init; }
    }

    public class Inverter : DeviceInfo
    {
        public Inverter()
        {
            DeviceClass = DeviceClass.Inverter;
        }

        public string? CustomName { get; init; } = string.Empty;
        public double MaxPvPowerWatts { get; init; }
        public bool Show { get; init; }
        public override string DisplayName => !string.IsNullOrWhiteSpace(CustomName) ? $"{CustomName} ({Model} #{Id})" : $"{Model} #{Id}";

        private InverterData? data;

        public InverterData? Data
        {
            get => data;
            set => Set(ref data, value);
        }

        private ThreePhasesData? threePhasesData;

        public ThreePhasesData? ThreePhasesData
        {
            get => threePhasesData;
            set => Set(ref threePhasesData, value);
        }
    }
}
