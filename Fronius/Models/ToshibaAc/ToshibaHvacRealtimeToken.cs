namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

/// <summary>
///     What <c>POST /api/RealTime/GetWebPubSubToken</c> answers: the Web PubSub client URL with the access token in
///     its query, when that token expires (15 minutes), and the groups it may listen to - one per air conditioner of
///     the account, named by device unique id.
/// </summary>
public class ToshibaHvacRealtimeToken
{
    [JsonPropertyName("Url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("ExpiresAt")]
    public DateTimeOffset ExpiresAt { get; set; }

    [JsonPropertyName("Groups")]
    public List<string> Groups { get; set; } = [];
}
