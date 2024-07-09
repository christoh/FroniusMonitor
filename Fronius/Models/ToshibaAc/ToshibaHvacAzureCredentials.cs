namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public class ToshibaHvacAzureCredentials : BindableBase
{
    private string hostName = string.Empty;

    private string deviceId = string.Empty;
    [JsonPropertyName("DeviceId")]
    public string DeviceId
    {
        get => deviceId;
        set => Set(ref deviceId, value);
    }

    [JsonPropertyName("HostName")]
    [JsonRequired]
    public string HostName
    {
        get => hostName;
        set => Set(ref hostName, value);
    }

    private string primaryKey = string.Empty;
    [JsonPropertyName("PrimaryKey")]
    public string PrimaryKey
    {
        get => primaryKey;
        set => Set(ref primaryKey, value);
    }

    private string secondaryKey = string.Empty;
    [JsonPropertyName("SecondaryKey")]
    public string SecondaryKey
    {
        get => secondaryKey;
        set => Set(ref secondaryKey, value);
    }

    private string sasToken = string.Empty;
    [JsonPropertyName("SasToken")]
    [JsonRequired]
    public string SasToken
    {
        get => sasToken;
        set => Set(ref sasToken, value);
    }

    private DateTime registerDate;
    [JsonPropertyName("RegisterDate")]
    public DateTime RegisterDate
    {
        get => registerDate;
        set => Set(ref registerDate, value);
    }
}