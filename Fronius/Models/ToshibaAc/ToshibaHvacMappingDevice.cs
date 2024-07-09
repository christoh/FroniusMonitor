namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public class ToshibaHvacMappingDevice : ToshibaHvacDeviceBase
{
    private string name = string.Empty;

    [JsonPropertyName("Name")]
    public string Name
    {
        get => name;
        set => Set(ref name, value);
    }

    private Guid acId;

    [JsonPropertyName("Id")]
    public Guid AcId
    {
        get => acId;
        set => Set(ref acId, value);
    }

    private Guid deviceUniqueId;

    [JsonPropertyName("DeviceUniqueId")]
    public override Guid DeviceUniqueId
    {
        get => deviceUniqueId;
        set => Set(ref deviceUniqueId, value);
    }

    private int acModelId;

    [JsonPropertyName("ACModelId")]
    public int AcModelId
    {
        get => acModelId;
        set => Set(ref acModelId, value);
    }

    private string description = string.Empty;

    [JsonPropertyName("Description")]
    public string Description
    {
        get => description;
        set => Set(ref description, value);
    }

    private string createdDate = string.Empty;

    [JsonPropertyName("CreatedDate")]
    public string CreatedDate
    {
        get => createdDate;
        set => Set(ref createdDate, value);
    }

    public override string ToString() => $"{Name} ({Description})";
}