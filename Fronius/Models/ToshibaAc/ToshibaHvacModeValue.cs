namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public partial class ToshibaHvacModeValue : BindableBase
{
    [ObservableProperty, JsonPropertyName("Mode"), JsonRequired]
    public partial ToshibaHvacOperatingMode Mode { get; set; }

    [ObservableProperty, JsonPropertyName("Temp"), JsonRequired]
    public partial sbyte TargetTemperatureCelsius { get; set; }

    [ObservableProperty, JsonPropertyName("FanSpeed"), JsonRequired]
    public partial ToshibaHvacFanSpeed FanSpeed { get; set; }
}