namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public class ToshibaHvacAzureCredentials : BindableBase
{
    [JsonPropertyName("DeviceId")]
    public string DeviceId
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [JsonPropertyName("HostName")]
    [JsonRequired]
    public string HostName
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [JsonPropertyName("PrimaryKey")]
    public string PrimaryKey
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [JsonPropertyName("SecondaryKey")]
    public string SecondaryKey
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [JsonPropertyName("SasToken")]
    [JsonRequired]
    public string SasToken
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [JsonPropertyName("RegisterDate")]
    public DateTime RegisterDate
    {
        get;
        set => Set(ref field, value);
    }
}