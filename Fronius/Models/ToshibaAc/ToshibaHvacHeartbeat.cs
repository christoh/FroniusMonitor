namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

internal class ToshibaHvacHeartbeat : BindableBase
{
    [JsonPropertyName("iTemp")]
    public byte? IndoorTemperatureCelsius
    {
        get => field == 0xff? null : field;
        set => Set(ref field, value ?? 0xff);
    }

    [JsonPropertyName("oTemp")]
    public byte? OutdoorTemperatureCelsius
    {
        get => field == 0xff ? null : field;
        set => Set(ref field, value ?? 0xff);
    }

    [JsonPropertyName("fcuTcTemp")]
    public byte? FcuTcTemperatureCelsius
    {
        get => field == 0xff ? null : field;
        set => Set(ref field, value ?? 0xff);
    }

    [JsonPropertyName("fcuTcjTemp")]
    public byte? FcuTcjTemperatureCelsius
    {
        get => field == 0xff ? null : field;
        set => Set(ref field, value ?? 0xff);
    }

    [JsonPropertyName("fcuFanRpm")]
    public byte? FcuFanRpm
    {
        get => field == 0xff? null : field;
        set => Set(ref field, value ?? 0xff);
    }

    [JsonPropertyName("cduTdTemp")]
    public byte? CduTdTemperatureCelsius
    {
        get => field == 0xff ? null :field;
        set => Set(ref field, value == null ? (byte)0xff : unchecked((byte)value));
    }
}