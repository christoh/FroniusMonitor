namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public class ToshibaHvacModeValue : BindableBase
{
    [JsonPropertyName("Mode")]
    [JsonRequired]
    public ToshibaHvacOperatingMode Mode
    {
        get;
        set => Set(ref field, value);
    }

    [JsonPropertyName("Temp")]
    [JsonRequired]
    public sbyte TargetTemperatureCelsius
    {
        get;
        set => Set(ref field, value);
    }

    [JsonPropertyName("FanSpeed")]
    [JsonRequired]
    public ToshibaHvacFanSpeed FanSpeed
    {
        get;
        set => Set(ref field, value);
    }
}