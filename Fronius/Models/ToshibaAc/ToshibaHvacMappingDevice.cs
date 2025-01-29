namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public class ToshibaHvacMappingDevice : ToshibaHvacDeviceBase
{
    [JsonPropertyName("Name")]
    public string Name
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [JsonPropertyName("Id")]
    public Guid AcId
    {
        get;
        set => Set(ref field, value);
    }

    [JsonPropertyName("DeviceUniqueId")]
    public override Guid DeviceUniqueId
    {
        get;
        set => Set(ref field, value);
    }

    [JsonPropertyName("ACModelId")]
    public int AcModelId
    {
        get;
        set => Set(ref field, value);
    }

    [JsonPropertyName("Description")]
    public string Description
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [JsonPropertyName("CreatedDate")]
    public string CreatedDate
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    public override string ToString() => $"{Name} ({Description})";
}