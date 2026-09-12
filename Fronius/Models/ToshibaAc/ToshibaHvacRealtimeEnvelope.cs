using System.Text.Json;

namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

/// <summary>
///     One frame of the <c>json.webpubsub.azure.v1</c> sub-protocol. <c>type</c> is <c>system</c> for
///     <c>connected</c> / <c>disconnected</c> events and <c>message</c> for a group message, whose <c>data</c> is a
///     <see cref="ToshibaHvacAzureSmMobileCommand" /> without a target list.
/// </summary>
public class ToshibaHvacRealtimeEnvelope
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("event")]
    public string? Event { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("connectionId")]
    public string? ConnectionId { get; set; }

    [JsonPropertyName("from")]
    public string? From { get; set; }

    [JsonPropertyName("group")]
    public string? Group { get; set; }

    [JsonPropertyName("dataType")]
    public string? DataType { get; set; }

    [JsonPropertyName("data")]
    public JsonElement? Data { get; set; }
}
