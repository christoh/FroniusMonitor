using System.Text.Json;

namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public class ToshibaHvacAzureSmMobileCommand : BindableBase
{
    [JsonPropertyName("sourceId")]
    [JsonRequired]
    public string DeviceUniqueId
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [JsonPropertyName("messageId")]
    public string MessageId
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [JsonPropertyName("targetId")]
    public IList<string> TargetIds
    {
        get;
        set => Set(ref field, value);
    } = Array.Empty<string>();

    [JsonPropertyName("cmd")]
    public string CommandName
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [JsonPropertyName("payload")]
    public JsonElement PayLoad
    {
        get;
        set => Set(ref field, value);
    }

    [JsonPropertyName("timeStamp")]
    public string TimeStamp
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [JsonPropertyName("timeZone")]
    public string? TimeZone
    {
        get;
        set => Set(ref field, value);
    }
}