namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public class ToshibaHvacMapping : BindableBase
{
    private Guid groupId;
    [JsonPropertyName("GroupId")]
    [JsonRequired]
    public Guid GroupId
    {
        get => groupId;
        set => Set(ref groupId, value);
    }

    private string groupName = string.Empty;
    [JsonPropertyName("GroupName")]
    public string GroupName
    {
        get => groupName;
        set => Set(ref groupName, value);
    }

    private Guid consumerId;
    [JsonPropertyName("ConsumerId")]
    public Guid ConsumerId
    {
        get => consumerId;
        set => Set(ref consumerId, value);
    }

    private string timeZone = string.Empty;

    [JsonPropertyName("TimeZone")]
    public string TimeZone
    {
        get => timeZone;
        set => Set(ref timeZone, value);
    }

    private ObservableCollection<ToshibaHvacMappingDevice> devices = new();
    [JsonPropertyName("ACList")]
    public ObservableCollection<ToshibaHvacMappingDevice> Devices
    {
        get => devices;
        set => Set(ref devices, value);
    }

    public override string ToString() => GroupName;
}