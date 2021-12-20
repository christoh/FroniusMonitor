using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models
{
    public enum MeterUsage : sbyte
    {
        MultipleConsumers,
        UniqueConsumer,
        Inverter,
    }

    public enum MeterLocation : sbyte
    {
        Grid,
        Load,
    }

    public class SmartMeter : DeviceInfo
    {
        public SmartMeter()
        {
            DeviceClass = DeviceClass.Meter;
        }

        public double L1Current { get; init; }
        public double L2Current { get; init; }
        public double L3Current { get; init; }
        public double TotalCurrent { get; init; }
        public string Manufacturer { get; init; } = string.Empty;
        public string MeterModel { get; init; } = string.Empty;
        public bool IsEnabled { get; init; }
        public double ReactiveEnergyConsumedWatts { get; init; }
        public double ReactiveEnergyProducedWatts { get; init; }
        public double ReactiveEnergyConsumedKiloWatts => ReactiveEnergyConsumedWatts / 1000;
        public double ReactiveEnergyProducedKiloWatts => ReactiveEnergyProducedWatts / 1000;
        public double RealEnergyConsumedWatts { get; init; }
        public double RealEnergyProducedWatts { get; init; }
        public double RealEnergyConsumedKiloWatts => RealEnergyConsumedWatts / 1000;
        public double RealEnergyProducedKiloWatts => RealEnergyProducedWatts / 1000;
        public double RealEnergyAbsolutePlusWatts { get; init; }
        public double RealEnergyAbsoluteMinusWatts { get; init; }
        public double RealEnergyAbsolutePlusKiloWatts => RealEnergyAbsolutePlusWatts / 1000;
        public double RealEnergyAbsoluteMinusKiloWatts => RealEnergyAbsoluteMinusWatts / 1000;
        public double Frequency { get; init; }
        public int MeterLocationCurrent { get; init; }
        public double L1ApparentPowerWatts { get; init; }
        public double L2ApparentPowerWatts { get; init; }
        public double L3ApparentPowerWatts { get; init; }
        public double TotalApparentPowerWatts { get; init; }
        public double L1ApparentPowerKiloWatts => L1ApparentPowerWatts / 1000;
        public double L2ApparentPowerKiloWatts => L2ApparentPowerWatts / 1000;
        public double L3ApparentPowerKiloWatts => L3ApparentPowerWatts / 1000;
        public double TotalApparentPowerKiloWatts => TotalApparentPowerWatts / 1000;
        public MeterLocation Location => MeterLocationCurrent == 0 ? MeterLocation.Grid : MeterLocation.Load;
        public MeterUsage Usage => MeterLocationCurrent < 2 ? MeterUsage.Inverter : MeterLocationCurrent > 255 ? MeterUsage.UniqueConsumer : MeterUsage.MultipleConsumers;

        public override string DisplayName => $"{Manufacturer} {MeterModel} #{Id}";
        public override string ToString() => DisplayName;
    }
}
