namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public partial class ToshibaHvacStatusDevice : ToshibaHvacDeviceBase
{
    [ObservableProperty, JsonPropertyName("ACId")]
    public partial Guid AcId { get; set; }

    [ObservableProperty, JsonPropertyName("Id")]
    public partial Guid Id { get; set; }

    [ObservableProperty, JsonPropertyName("OnOff"), NotifyPropertyChangedFor(nameof(IsTurnedOn))]
    public partial ToshibaHvacPowerState PowerState { get; set; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(SerialNumber)), JsonPropertyName("ACDeviceUniqueId")]
    public override partial Guid DeviceUniqueId { get; set; }

    [ObservableProperty, JsonPropertyName("VersionInfo")]
    public partial string VersionInfo { get; set; } = string.Empty;

    [ObservableProperty, JsonPropertyName("Model")]
    public partial int AcModelId { get; set; }

    [ObservableProperty, JsonPropertyName("UpdatedDate")]
    public partial DateTime UpdatedDate { get; set; }

    [ObservableProperty, JsonPropertyName("Lat")]
    public partial double Latitude { get; set; }

    [ObservableProperty, JsonPropertyName("Long")]
    public partial double Longitude { get; set; }

    [ObservableProperty, JsonPropertyName("IsMapped")]
    public partial bool IsMapped { get; set; }

    [ObservableProperty, JsonPropertyName("FirstConnectionTime")]
    public partial DateTime FirstConnectionTime { get; set; }

    [ObservableProperty, JsonPropertyName("LastConnectionTime")]
    public partial DateTime LastConnectionTime { get; set; }

    [ObservableProperty, JsonPropertyName("ConsumerMasterId")]
    public partial string ConsumerMasterId { get; set; } = string.Empty;

    [ObservableProperty, JsonPropertyName("PartitionKey")]
    public partial Guid PartitionKey { get; set; }
}