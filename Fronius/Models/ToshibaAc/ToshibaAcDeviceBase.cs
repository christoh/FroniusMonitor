﻿namespace De.Hochstaetter.Fronius.Models.ToshibaAc
{
    public abstract class ToshibaAcDeviceBase : BindableBase, ISwitchable
    {
        private ToshibaAcStateData state = new();

        [JsonPropertyName("ACStateData")]
        public ToshibaAcStateData State
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

        private ObservableCollection<ToshibaAcModeValue> modes = new();

        [JsonPropertyName("ModeValues")]
        public ObservableCollection<ToshibaAcModeValue> Modes
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
