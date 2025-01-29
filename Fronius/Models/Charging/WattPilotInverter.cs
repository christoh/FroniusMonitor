namespace De.Hochstaetter.Fronius.Models.Charging;

public class WattPilotInverter : BindableBase, IHaveDisplayName
{
    [WattPilot("id")]
    public string? Id
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("paired")]
    public bool? IsPaired
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("deviceFamily")]
    public string? DeviceFamily
    {
        get;
        set => Set(ref field, value);
    }

    public string DisplayName => Label ?? string.Empty;
    public override string ToString() => DisplayName;

    [WattPilot("label")]
    public string? Label
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(DisplayName)));
    }

    [WattPilot("model")]
    public string? ModelName
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("commonName")]
    public string? CommonName
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("connected")]
    public bool? IsConnected
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("reachableMdns")]
    public bool? HasMdns
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("reachableUdp")]
    public bool? HasUdp
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("reachableHttp")]
    public bool HasHttp
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("status")]
    public byte? StatusCode
    {
        get;
        set => Set(ref field, value);
    }

    public string? Message
    {
        get;
        set => Set(ref field, value);
    }
}