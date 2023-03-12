namespace De.Hochstaetter.Fronius.Models.ToshibaAc
{
    public class ToshibaAcModeValue : BindableBase
    {
        private ToshibaAcOperatingMode mode;
        [JsonPropertyName("Mode")]
        [JsonRequired]
        public ToshibaAcOperatingMode Mode
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

        private ToshibaAcFanSpeed fanSpeed;
        [JsonPropertyName("FanSpeed")]
        [JsonRequired]
        public ToshibaAcFanSpeed FanSpeed
        {
            get => fanSpeed;
            set => Set(ref fanSpeed, value);
        }
    }
}
