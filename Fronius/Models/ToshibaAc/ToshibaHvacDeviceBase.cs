namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public abstract partial class ToshibaHvacDeviceBase : BindableBase, ISwitchable
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsTurnedOn)), JsonPropertyName("ACStateData")]
    public partial ToshibaHvacStateData State { get; set; } = new();

    [ObservableProperty, JsonPropertyName("FirmwareVersion")]
    public partial Version FirmwareVersion { get; set; } = default!;

    [ObservableProperty, JsonPropertyName("MeritFeature")]
    public partial ushort MeritFeature { get; set; }

    [ObservableProperty, JsonPropertyName("ModeValues")]
    public partial ObservableCollection<ToshibaHvacModeValue> Modes { get; set; } = new();

    public abstract Guid DeviceUniqueId { get; set; }

    public bool IsSwitchingEnabled => false;
    public bool? IsTurnedOn => State.IsTurnedOn;
    public bool CanSwitch => true;

    public Task TurnOnOff(bool turnOn)
    {
        throw new NotImplementedException();
    }

    [JsonIgnore]
    public bool IsPresent => true;

    [JsonIgnore]
    public string Manufacturer => "Toshiba";

    [JsonIgnore]
    public string Model => "HVAC";

    [JsonIgnore]
    public string SerialNumber => DeviceUniqueId.ToString("N");
}