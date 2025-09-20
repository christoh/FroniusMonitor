// ReSharper disable StringLiteralTypo

using Newtonsoft.Json;

namespace De.Hochstaetter.Fronius.Models.Charging;

public partial class WattPilotWifiInfo : BindableBase, IHaveDisplayName
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    [JsonProperty("ssid")]
    [WattPilot("ssid")]
    public partial string? Ssid { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Type))]
    [JsonProperty("b")]
    [WattPilot("b")]
    [WattPilot("f", 2)]
    public partial bool? IsB { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Type))]
    [JsonProperty("g")]
    [WattPilot("f", 3)]
    [WattPilot("g")]
    public partial bool? IsG { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Type))]
    [WattPilot("f", 4)]
    [JsonProperty("n")]
    [WattPilot("n")]
    public partial bool? IsN { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public IPAddress? IpV4Address
    {
        get => string.IsNullOrWhiteSpace(IpV4AddressString) ? null : IPAddress.Parse(IpV4AddressString);
        set => IpV4AddressString = value?.ToString();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IpV4Address))]
    [JsonPropertyName("ipV4Address")]
    [WattPilot("ip")]
    [JsonProperty("ip")]
    public partial string? IpV4AddressString { get; set; }

    [ObservableProperty]
    [WattPilot("f", 9)]
    public partial string? CountryCode { get; set; }

    [ObservableProperty]
    [JsonProperty("channel")]
    [WattPilot("channel")]
    public partial int? Channel { get; set; }

    [ObservableProperty]
    [JsonProperty("encryptionType")]
    [WattPilot("encryptionType")]
    public partial WifiEncryption? Encryption { get; set; }

    [ObservableProperty]
    [JsonProperty("pairwiseCipher")]
    [WattPilot("pairwiseCipher")]
    [WattPilot("f", 0)]
    public partial WifiCipher? PairwiseCipher { get; set; }

    [ObservableProperty]
    [JsonProperty("groupCipher")]
    [WattPilot("groupCipher")]
    [WattPilot("f", 1)]
    public partial WifiCipher? GroupCipher { get; set; }

    [ObservableProperty]
    [JsonProperty("lr")]
    [WattPilot("lr")]
    [WattPilot("f", 5)]
    public partial bool? SupportsLowRate { get; set; }

    [ObservableProperty]
    [JsonProperty("wps")]
    [WattPilot("wps")]
    [WattPilot("f", 6)]
    public partial bool? AllowWps { get; set; }

    [ObservableProperty]
    [JsonProperty("ftmResponder")]
    [WattPilot("ftmResponder")]
    [WattPilot("f", 7)]
    public partial bool? IsFtmResponder { get; set; }

    [ObservableProperty]
    [JsonProperty("ftmInitiator")]
    [WattPilot("ftmInitiator")]
    [WattPilot("f", 8)]
    public partial bool? IsFtmInitiator { get; set; }

    [ObservableProperty]
    [JsonProperty("bssid")]
    [WattPilot("bssid")]
    [JsonPropertyName("macAddress")]
    public partial string? MacAddressString { get; set; }

    [ObservableProperty]
    [JsonProperty("rssi")]
    [WattPilot("rssi")]
    public partial int? WifiSignal { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public IPAddress? NetMask
    {
        get => !string.IsNullOrWhiteSpace(NetMaskString) ? IPAddress.Parse(NetMaskString) : null;
        set => NetMaskString = value?.ToString();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NetMask))]
    [JsonPropertyName("netMask")]
    [JsonProperty("netmask")]
    [WattPilot("netmask")]
    public partial string? NetMaskString { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public IPAddress? Gateway
    {
        get => !string.IsNullOrWhiteSpace(GatewayString) ? IPAddress.Parse(GatewayString) : null;
        set => GatewayString = value?.ToString();
    }

    [ObservableProperty]
    [JsonPropertyName("gateway")]
    [NotifyPropertyChangedFor(nameof(Gateway))]
    [JsonProperty("gw")]
    [WattPilot("gw")]
    public partial string? GatewayString { get; set; }


    public string Type
    {
        get
        {
            var isFirst = true;
            var builder = new StringBuilder(10);
            builder.Append("802.11 ");
            Add(IsB, 'b');
            Add(IsG, 'g');
            Add(IsN, 'n');

            return builder.ToString();

            void Add(bool? hasIt, char name)
            {
                if (hasIt is true)
                {
                    if (!isFirst)
                    {
                        builder.Append('/');
                    }

                    builder.Append(name);
                    isFirst = false;
                }
            }
        }
    }

    public override string ToString() => Ssid ?? Resources.HiddenWifi;
    public string DisplayName => ToString();
}