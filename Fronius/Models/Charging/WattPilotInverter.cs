namespace De.Hochstaetter.Fronius.Models.Charging;

public class WattPilotInverter : BindableBase, IHaveDisplayName
{
    private string? id;
    [WattPilot("id")]
    public string? Id
    {
        get => id;
        set => Set(ref id, value);
    }

    private bool? isPaired;
    [WattPilot("paired")]
    public bool? IsPaired
    {
        get => isPaired;
        set => Set(ref isPaired, value);
    }

    private string? deviceFamily;
    [WattPilot("deviceFamily")]
    public string? DeviceFamily
    {
        get => deviceFamily;
        set => Set(ref deviceFamily, value);
    }

    public string DisplayName => Label ?? string.Empty;
    public override string ToString() => DisplayName;

    private string? label;
    [WattPilot("label")]
    public string? Label
    {
        get => label;
        set => Set(ref label, value, () => NotifyOfPropertyChange(nameof(DisplayName)));
    }

    private string? modelName;
    [WattPilot("model")]
    public string? ModelName
    {
        get => modelName;
        set => Set(ref modelName, value);
    }

    private string? commonName;
    [WattPilot("commonName")]
    public string? CommonName
    {
        get => commonName;
        set => Set(ref commonName, value);
    }

    private bool? isConnected;
    [WattPilot("connected")]
    public bool? IsConnected
    {
        get => isConnected;
        set => Set(ref isConnected, value);
    }

    private bool? hasMdns;
    [WattPilot("reachableMdns")]
    public bool? HasMdns
    {
        get => hasMdns;
        set => Set(ref hasMdns, value);
    }

    private bool? hasUdp;
    [WattPilot("reachableUdp")]
    public bool? HasUdp
    {
        get => hasUdp;
        set => Set(ref hasUdp, value);
    }

    private bool hasHttp;
    [WattPilot("reachableHttp")]
    public bool HasHttp
    {
        get => hasHttp;
        set => Set(ref hasHttp, value);
    }

    private byte? statusCode;
    [WattPilot("status")]
    public byte? StatusCode
    {
        get => statusCode;
        set => Set(ref statusCode, value);
    }

    private string? message;

    public string? Message
    {
        get => message;
        set => Set(ref message, value);
    }
}