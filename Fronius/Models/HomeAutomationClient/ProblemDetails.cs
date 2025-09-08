namespace De.Hochstaetter.Fronius.Models.HomeAutomationClient;

public partial class ProblemDetails : BindableBase
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    [JsonPropertyName("errors")]
    public Dictionary<string, List<string>>? Errors { get; set; }

    [JsonPropertyName("status")]
    public HttpStatusCode? Status { get; set; }
}