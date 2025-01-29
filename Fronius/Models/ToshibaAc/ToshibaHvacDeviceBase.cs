namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public abstract class ToshibaHvacDeviceBase : BindableBase, ISwitchable
{
    [JsonPropertyName("ACStateData")]
    public ToshibaHvacStateData State
    {
        get;
        set => Set(ref field, value);
    } = new();

    [JsonPropertyName("FirmwareVersion")]
    public Version FirmwareVersion
    {
        get;
        set => Set(ref field, value);
    } = default!;

    [JsonPropertyName("MeritFeature")]
    public ushort MeritFeature
    {
        get;
        set => Set(ref field, value);
    }

    [JsonPropertyName("ModeValues")]
    public ObservableCollection<ToshibaHvacModeValue> Modes
    {
        get;
        set => Set(ref field, value);
    } = new();

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