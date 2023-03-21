namespace De.Hochstaetter.Fronius.Models.ToshibaAc
{
    public class ToshibaHvacModeValue : BindableBase
    {
        private ToshibaHvacOperatingMode mode;
        [JsonPropertyName("Mode")]
        [JsonRequired]
        public ToshibaHvacOperatingMode Mode
        {
            get => mode;
            set => Set(ref mode, value);
        }

        private sbyte targetTemperatureCelsius;
        [JsonPropertyName("Temp")]
        [JsonRequired]
        public sbyte TargetTemperatureCelsius
        {
            get => targetTemperatureCelsius;
            set => Set(ref targetTemperatureCelsius, value);
        }

        private ToshibaHvacFanSpeed fanSpeed;
        [JsonPropertyName("FanSpeed")]
        [JsonRequired]
        public ToshibaHvacFanSpeed FanSpeed
        {
            get => fanSpeed;
            set => Set(ref fanSpeed, value);
        }
    }
}
