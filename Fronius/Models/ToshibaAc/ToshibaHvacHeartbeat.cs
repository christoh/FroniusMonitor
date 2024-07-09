namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

internal class ToshibaHvacHeartbeat : BindableBase
{
    private byte indoorTemperatureCelsius;
    [JsonPropertyName("iTemp")]
    public byte? IndoorTemperatureCelsius
    {
        get => indoorTemperatureCelsius == 0xff? null : indoorTemperatureCelsius;
        set => Set(ref indoorTemperatureCelsius, value ?? 0xff);
    }

    private byte outdoorTemperatureCelsius;
    [JsonPropertyName("oTemp")]
    public byte? OutdoorTemperatureCelsius
    {
        get => outdoorTemperatureCelsius == 0xff ? null : outdoorTemperatureCelsius;
        set => Set(ref outdoorTemperatureCelsius, value ?? 0xff);
    }

    private byte fcuTcTemperatureCelsius;
    [JsonPropertyName("fcuTcTemp")]
    public byte? FcuTcTemperatureCelsius
    {
        get => fcuTcTemperatureCelsius == 0xff ? null : fcuTcTemperatureCelsius;
        set => Set(ref fcuTcTemperatureCelsius, value ?? 0xff);
    }

    private byte fcuTcjTemperatureCelsius;
    [JsonPropertyName("fcuTcjTemp")]
    public byte? FcuTcjTemperatureCelsius
    {
        get => fcuTcjTemperatureCelsius == 0xff ? null : fcuTcjTemperatureCelsius;
        set => Set(ref fcuTcjTemperatureCelsius, value ?? 0xff);
    }

    private byte fcuFanRpm;
    [JsonPropertyName("fcuFanRpm")]
    public byte? FcuFanRpm
    {
        get => fcuFanRpm == 0xff? null : fcuFanRpm;
        set => Set(ref fcuFanRpm, value ?? 0xff);
    }

    private byte cduTdTemperatureCelsius;
    [JsonPropertyName("cduTdTemp")]
    public byte? CduTdTemperatureCelsius
    {
        get => cduTdTemperatureCelsius == 0xff ? null :cduTdTemperatureCelsius;
        set => Set(ref cduTdTemperatureCelsius, value == null ? (byte)0xff : unchecked((byte)value));
    }
}