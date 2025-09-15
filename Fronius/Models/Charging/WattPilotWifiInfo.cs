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

    [ObservableProperty]
    [JsonProperty("ip")]
    [WattPilot("ip")]
    public partial IPAddress? IpV4Address { get; set; }

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
    public partial string? MacAddressString { get; set; }

    [ObservableProperty]
    [JsonProperty("rssi")]
    [WattPilot("rssi")]
    public partial int? WifiSignal { get; set; }

    [ObservableProperty]
    [JsonProperty("netmask")]
    [WattPilot("netmask")]
    public partial IPAddress? NetMask { get; set; }

    [ObservableProperty]
    [JsonProperty("gw")]
    [WattPilot("gw")]
    public partial IPAddress? Gateway { get; set; }

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