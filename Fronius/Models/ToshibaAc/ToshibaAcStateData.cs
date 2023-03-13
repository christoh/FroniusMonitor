namespace De.Hochstaetter.Fronius.Models.ToshibaAc
{
    public class ToshibaAcStateData : BindableBase
    {
        private IList<byte> stateData = Array.Empty<byte>();

        public IList<byte> StateData
        {
            get => stateData;
            set => Set(ref stateData, value, NotifyStateDataProperties);
        }

        public bool IsTurnedOn
        {
            get => StateData[0] == 0x30;
            set => SetStateData(0, value ? (byte)0x30 : (byte)0x31);
        }

        public ToshibaAcOperatingMode Mode
        {
            get => (ToshibaAcOperatingMode)StateData[1];
            set => SetStateData(1, (byte)value);
        }

        public sbyte TargetTemperatureCelsius
        {
            get => unchecked((sbyte)StateData[2]);
            set => SetStateData(2, unchecked((byte)value));
        }

        public ToshibaAcFanSpeed FanSpeed
        {
            get => (ToshibaAcFanSpeed)StateData[3];
            set => SetStateData(3, (byte)value);
        }

        public byte PowerLimit
        {
            get => StateData[5];
            set => SetStateData(5, value);
        }

        public ToshibaAcPowerSetting PowerSetting
        {
            get => (ToshibaAcPowerSetting)StateData[6];
            set => SetStateData(6, (byte)value);
        }

        public sbyte? CurrentIndoorTemperatureCelsius => unchecked(StateData[8] == 127 ? null : (sbyte)StateData[8]);

        public sbyte? CurrentOutdoorTemperatureCelsius => unchecked(StateData[9] == 127 ? null : (sbyte)StateData[9]);

        public override string ToString() => StateData.Aggregate(new StringBuilder(), (c, n) => c.Append($"{n:x2}")).ToString();

        internal void UpdateStateData(ToshibaAcStateData update)
        {
            for (var i = 0; i < Math.Min(update.StateData.Count, StateData.Count); i++)
            {
                if (update.StateData[i] != 255)
                {
                    StateData[i] = update.StateData[i];
                }
            }

            NotifyStateDataProperties();
        }

        internal void UpdateHeartBeatData(ToshibaAcHeartbeat heartbeat)
        {
            StateData[8] = heartbeat.IndoorTemperatureCelsius ?? StateData[8];
            StateData[9] = heartbeat.OutdoorTemperatureCelsius ?? StateData[9];

            if (heartbeat.IndoorTemperatureCelsius.HasValue)
            {
                NotifyOfPropertyChange(nameof(CurrentIndoorTemperatureCelsius));
            }

            if (heartbeat.OutdoorTemperatureCelsius.HasValue)
            {
                NotifyOfPropertyChange(nameof(CurrentOutdoorTemperatureCelsius));
            }
        }

        private void NotifyStateDataProperties()
        {
            NotifyOfPropertyChange(nameof(ToshibaAcOperatingMode));
            NotifyOfPropertyChange(nameof(IsTurnedOn));
            NotifyOfPropertyChange(nameof(TargetTemperatureCelsius));
            NotifyOfPropertyChange(nameof(FanSpeed));
            NotifyOfPropertyChange(nameof(Mode));
            NotifyOfPropertyChange(nameof(PowerLimit));
            NotifyOfPropertyChange(nameof(CurrentIndoorTemperatureCelsius));
            NotifyOfPropertyChange(nameof(CurrentOutdoorTemperatureCelsius));
            NotifyOfPropertyChange(nameof(PowerSetting));
        }

        private void SetStateData(int index, byte value, [CallerMemberName] string? propertyName = null)
        {
            if (value != StateData[index])
            {
                StateData[index] = value;
                NotifyOfPropertyChange(propertyName);
            }
        }
    }
}
