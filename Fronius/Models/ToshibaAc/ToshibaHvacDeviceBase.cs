namespace De.Hochstaetter.Fronius.Models.ToshibaAc
{
    public abstract class ToshibaHvacDeviceBase : BindableBase, ISwitchable
    {
        private ToshibaHvacStateData state = new();

        [JsonPropertyName("ACStateData")]
        public ToshibaHvacStateData State
        {
            get => state;
            set => Set(ref state, value);
        }

        private Version firmwareVersion = default!;

        [JsonPropertyName("FirmwareVersion")]
        public Version FirmwareVersion
        {
            get => firmwareVersion;
            set => Set(ref firmwareVersion, value);
        }

        private ushort meritFeature;

        [JsonPropertyName("MeritFeature")]
        public ushort MeritFeature
        {
            get => meritFeature;
            set => Set(ref meritFeature, value);
        }

        private ObservableCollection<ToshibaHvacModeValue> modes = new();

        [JsonPropertyName("ModeValues")]
        public ObservableCollection<ToshibaHvacModeValue> Modes
        {
            get => modes;
            set => Set(ref modes, value);
        }

        public bool IsSwitchingEnabled => false;
        public bool? IsTurnedOn => State.IsTurnedOn;
        public bool CanSwitch => true;

        public Task TurnOnOff(bool turnOn)
        {
            throw new NotImplementedException();
        }
    }
}
