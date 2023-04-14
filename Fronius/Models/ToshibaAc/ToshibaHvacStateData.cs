namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public class ToshibaHvacStateData : BindableBase
{
    private static readonly IReadOnlyDictionary<byte, string> stateDataMap = new Dictionary<byte, string>
    {
        { 0, nameof(IsTurnedOn) },
        { 1, nameof(Mode) },
        { 2, nameof(TargetTemperatureCelsius) },
        { 3, nameof(FanSpeed) },
        { 4, nameof(SwingMode) },
        { 5, nameof(PowerLimit) },
        { 6, nameof(MeritFeaturesA) },
        { 8, nameof(CurrentIndoorTemperatureCelsius) },
        { 9, nameof(CurrentOutdoorTemperatureCelsius) },
        { 15, nameof(WifiLedStatus) },
    };

    public ToshibaHvacStateData()
    {
        stateData = new byte[19];
        Array.Fill<byte>((byte[])stateData, 0xff);
    }

    private IList<byte> stateData;

    public IList<byte> StateData
    {
        get => stateData;
        set => Set(ref stateData, value, () => stateDataMap.Values.Apply(propertyList => propertyList.Split('|').Apply(NotifyOfPropertyChange)));
    }

    public bool IsTurnedOn
    {
        get => StateData[0] == 0x30;
        set => SetStateData(0, value ? (byte)0x30 : (byte)0x31);
    }

    public ToshibaHvacOperatingMode Mode
    {
        get => (ToshibaHvacOperatingMode)StateData[1];
        set => SetStateData(1, (byte)value);
    }

    public sbyte? TargetTemperatureCelsius
    {
        get => ToTemperature(StateData[2]);
        set => SetStateData(2, ToByte(value));
    }

    public ToshibaHvacFanSpeed FanSpeed
    {
        get => (ToshibaHvacFanSpeed)StateData[3];
        set => SetStateData(3, (byte)value);
    }

    public ToshibaHvacSwingMode SwingMode
    {
        get => (ToshibaHvacSwingMode)StateData[4];
        set => SetStateData(4, (byte)value);
    }

    public byte PowerLimit
    {
        get => StateData[5];
        set => SetStateData(5, value);
    }

    public ToshibaHvacMeritFeaturesA MeritFeaturesA
    {
        get => (ToshibaHvacMeritFeaturesA)(StateData[6] & 0xf);

        set
        {
            if (value != MeritFeaturesA)
            {
                StateData[6] = unchecked((byte)((StateData[6] & 0xf0) | ((byte)value & 0xf)));
                NotifyOfPropertyChange();
            }
        }
    }

    public sbyte? CurrentIndoorTemperatureCelsius => ToTemperature(StateData[8]);

    public sbyte? CurrentOutdoorTemperatureCelsius => ToTemperature(StateData[9]);

    public ToshibaHvacWifiLedStatus WifiLedStatus
    {
        get => (ToshibaHvacWifiLedStatus)StateData[15];
        set => SetStateData(15, (byte)value);
    }

    public override string ToString() => StateData.Aggregate(new StringBuilder(StateData.Count << 1), (c, n) => c.Append($"{n:x2}")).ToString();

    internal void UpdateStateData(ToshibaHvacStateData update)
    {
        for (byte i = 0; i < new[] { update.StateData.Count, StateData.Count, 255 }.Min(); i++)
        {
            if (update.StateData[i] == 255)
            {
                continue;
            }

            StateData[i] = update.StateData[i];

            if (stateDataMap.TryGetValue(i, out var propertyName))
            {
                NotifyOfPropertyChange(propertyName);
            }
        }
    }

    internal void UpdateHeartBeatData(ToshibaHvacHeartbeat heartbeat)
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

    private void SetStateData(int index, byte value, [CallerMemberName] string? propertyName = null)
    {
        if (value != StateData[index])
        {
            StateData[index] = value;
            NotifyOfPropertyChange(propertyName);
        }
    }

    private static sbyte? ToTemperature(byte value) => value switch
    {
        0x7f => null,
        0x7e => -1,
        _ => unchecked((sbyte)value)
    };

    private static byte ToByte(sbyte? value) => value switch
    {
        null => 0x7f,
        -1 => 0x7e,
        _ => unchecked((byte)value)
    };
}
