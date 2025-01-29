namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public class ToshibaHvacStatusDevice : ToshibaHvacDeviceBase
{
    [JsonPropertyName("ACId")]
    public Guid AcId
    {
        get;
        set => Set(ref field, value);
    }

    [JsonPropertyName("Id")]
    public Guid Id
    {
        get;
        set => Set(ref field, value);
    }

    [JsonPropertyName("OnOff")]
    public ToshibaHvacPowerState PowerState
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(IsTurnedOn)));
    }

    [JsonPropertyName("ACDeviceUniqueId")]
    public override Guid DeviceUniqueId
    {
        get;
        set => Set(ref field, value);
    }

    [JsonPropertyName("VersionInfo")]
    public string VersionInfo
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [JsonPropertyName("Model")]
    public int AcModelId
    {
        get;
        set => Set(ref field, value);
    }

    [JsonPropertyName("UpdatedDate")]
    public DateTime UpdatedDate
    {
        get;
        set => Set(ref field, value);
    }

    [JsonPropertyName("Lat")]
    public double Latitude
    {
        get;
        set => Set(ref field, value);
    }

    [JsonPropertyName("Long")]
    public double Longitude
    {
        get;
        set => Set(ref field, value);
    }

    [JsonPropertyName("IsMapped")]
    public bool IsMapped
    {
        get;
        set => Set(ref field, value);
    }

    [JsonPropertyName("FirstConnectionTime")]
    public DateTime FirstConnectionTime
    {
        get;
        set => Set(ref field, value);
    }

    [JsonPropertyName("LastConnectionTime")]
    public DateTime LastConnectionTime
    {
        get;
        set => Set(ref field, value);
    }

    [JsonPropertyName("ConsumerMasterId")]
    public string ConsumerMasterId
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [JsonPropertyName("PartitionKey")]
    public Guid PartitionKey
    {
        get;
        set => Set(ref field, value);
    }
}