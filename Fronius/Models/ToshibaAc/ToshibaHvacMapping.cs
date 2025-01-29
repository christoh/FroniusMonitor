namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public class ToshibaHvacMapping : BindableBase
{
    [JsonPropertyName("GroupId")]
    [JsonRequired]
    public Guid GroupId
    {
        get;
        set => Set(ref field, value);
    }

    [JsonPropertyName("GroupName")]
    public string GroupName
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [JsonPropertyName("ConsumerId")]
    public Guid ConsumerId
    {
        get;
        set => Set(ref field, value);
    }

    [JsonPropertyName("TimeZone")]
    public string TimeZone
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [JsonPropertyName("ACList")]
    public ObservableCollection<ToshibaHvacMappingDevice> Devices
    {
        get;
        set => Set(ref field, value);
    } = new();

    public override string ToString() => GroupName;
}