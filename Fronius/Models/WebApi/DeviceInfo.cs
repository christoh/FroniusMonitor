namespace De.Hochstaetter.Fronius.Models.WebApi;

public class DeviceInfo
{
    [JsonPropertyName("deviceType")]
    public string DeviceType { get; set; } = string.Empty;
    
    [JsonPropertyName("interfaces")]
    public List<string> Interfaces { get; set; } = [];
    
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;
    
    [JsonPropertyName("serviceType")]
    public string? ServiceType { get; set; } = string.Empty;
    
    [JsonPropertyName("credentialType")]
    public string? CredentialType { get; set; } = string.Empty;
}
