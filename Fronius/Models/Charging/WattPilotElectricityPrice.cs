using Newtonsoft.Json;

namespace De.Hochstaetter.Fronius.Models.Charging
{
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public class WattPilotElectricityPrice : BindableBase, IElectricityPrice, ICloneable
    {
        [JsonProperty("marketprice")]
        [WattPilot("marketprice")]
        public decimal CentsPerKiloWattHour
        {
            get;
            set => Set(ref field, value);
        }

        [JsonProperty("start")]
        [WattPilot("start")]
        public long StartSeconds
        {
            get;
            set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(StartTime)));
        }

        [Newtonsoft.Json.JsonIgnore]
        public DateTime StartTime
        {
            get => DateTime.UnixEpoch.AddSeconds(StartSeconds);
            set => StartSeconds = (long)Math.Round((value - DateTime.UnixEpoch).TotalSeconds, MidpointRounding.AwayFromZero);
        }

        [JsonProperty("end")]
        [WattPilot("end")]
        public long EndSeconds
        {
            get;
            set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(EndTime)));
        }

        [Newtonsoft.Json.JsonIgnore]
        public DateTime EndTime
        {
            get => DateTime.UnixEpoch.AddSeconds(EndSeconds);
            set => EndSeconds = (long)Math.Round((value - DateTime.UnixEpoch).TotalSeconds, MidpointRounding.AwayFromZero);
        }


        public object Clone() => MemberwiseClone();
        
        #if DEBUG
        public override string ToString() => $"{StartTime.ToLocalTime():g}: {CentsPerKiloWattHour:N2} ct/kWh";
        #endif
    }
}
