using System.Text.Json;

namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public partial class ToshibaHvacAzureSmMobileCommand : BindableBase
{
    [ObservableProperty]
    [JsonPropertyName("sourceId")]
    [JsonRequired]
    public partial string DeviceUniqueId { get; set; } = string.Empty;

    [ObservableProperty]
    [JsonPropertyName("messageId")]
    public partial string MessageId { get; set; } = string.Empty;

    [ObservableProperty]
    [JsonPropertyName("targetId")]
    public partial IList<string> TargetIds { get; set; } = Array.Empty<string>();

    [ObservableProperty]
    [JsonPropertyName("cmd")]
    public partial string CommandName { get; set; } = string.Empty;

    [ObservableProperty]
    [JsonPropertyName("payload")]
    public partial JsonElement PayLoad { get; set; }

    [ObservableProperty]
    [JsonPropertyName("timeStamp")]
    public partial string TimeStamp { get; set; } = string.Empty;

    [ObservableProperty]
    [JsonPropertyName("timeZone")]
    public partial string? TimeZone { get; set; }
}