namespace De.Hochstaetter.Fronius.Models.Charging;

public partial class WattPilotInverter : BindableBase, IHaveDisplayName
{
    [ObservableProperty]
    [WattPilot("id")]
    public partial string? Id { get; set; }

    [ObservableProperty]
    [WattPilot("paired")]
    public partial bool? IsPaired { get; set; }

    [ObservableProperty]
    [WattPilot("deviceFamily")]
    public partial string? DeviceFamily { get; set; }

    public string DisplayName => Label ?? string.Empty;
    public override string ToString() => DisplayName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    [WattPilot("label")]
    public partial string? Label { get; set; }

    [ObservableProperty]
    [WattPilot("model")]
    public partial string? ModelName { get; set; }

    [ObservableProperty]
    [WattPilot("commonName")]
    public partial string? CommonName { get; set; }

    [ObservableProperty]
    [WattPilot("connected")]
    public partial bool? IsConnected { get; set; }

    [ObservableProperty]
    [WattPilot("reachableMdns")]
    public partial bool? HasMdns { get; set; }

    [ObservableProperty]
    [WattPilot("reachableUdp")]
    public partial bool? HasUdp { get; set; }

    [ObservableProperty]
    [WattPilot("reachableHttp")]
    public partial bool HasHttp { get; set; }

    [ObservableProperty]
    [WattPilot("status")]
    public partial byte? StatusCode { get; set; }

    [ObservableProperty]
    public partial string? Message { get; set; }
}