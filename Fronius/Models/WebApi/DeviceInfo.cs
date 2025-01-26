namespace De.Hochstaetter.Fronius.Models.WebApi;

public class DeviceInfo
{
    public string DeviceType { get; set; } = string.Empty;
    public IEnumerable<string> Interfaces { get; set; } = [];
    public string DisplayName { get; set; } = string.Empty;
    public string? ServiceType { get; set; } = string.Empty;
    public string? CredentialType { get; set; } = string.Empty;
}
