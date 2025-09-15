namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public partial class ToshibaHvacAzureCredentials : BindableBase
{
    [ObservableProperty]
    [JsonPropertyName("DeviceId")]
    public partial string DeviceId { get; set; } = string.Empty;

    [ObservableProperty]
    [JsonPropertyName("HostName")]
    [JsonRequired]
    public partial string HostName { get; set; } = string.Empty;

    [ObservableProperty]
    [JsonPropertyName("PrimaryKey")]
    public partial string PrimaryKey { get; set; } = string.Empty;

    [ObservableProperty]
    [JsonPropertyName("SecondaryKey")]
    public partial string SecondaryKey { get; set; } = string.Empty;

    [ObservableProperty]
    [JsonPropertyName("SasToken")]
    [JsonRequired]
    public partial string SasToken { get; set; } = string.Empty;

    [ObservableProperty]
    [JsonPropertyName("RegisterDate")]
    public partial DateTime RegisterDate { get; set; }
}