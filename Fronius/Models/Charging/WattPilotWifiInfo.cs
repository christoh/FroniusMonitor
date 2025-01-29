// ReSharper disable StringLiteralTypo

using Newtonsoft.Json;

namespace De.Hochstaetter.Fronius.Models.Charging;

public class WattPilotWifiInfo : BindableBase, IHaveDisplayName
{
    [JsonProperty("ssid")]
    [WattPilot("ssid")]
    public string? Ssid
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(DisplayName));
    }

    [JsonProperty("b")]
    [WattPilot("b")]
    [WattPilot("f", 2)]
    public bool? IsB
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(Type)));
    }

    [JsonProperty("g")]
    [WattPilot("f", 3)]
    [WattPilot("g")]
    public bool? IsG
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(Type)));
    }

    [WattPilot("f", 4)]
    [JsonProperty("n")]
    [WattPilot("n")]
    public bool? IsN
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(Type)));
    }

    [JsonProperty("ip")]
    [WattPilot("ip")]
    public IPAddress? IpV4Address
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("f", 9)]
    public string? CountryCode
    {
        get;
        set => Set(ref field, value);
    }

    [JsonProperty("channel")]
    [WattPilot("channel")]
    public int? Channel
    {
        get;
        set => Set(ref field, value);
    }

    [JsonProperty("encryptionType")]
    [WattPilot("encryptionType")]
    public WifiEncryption? Encryption
    {
        get;
        set => Set(ref field, value);
    }

    [JsonProperty("pairwiseCipher")]
    [WattPilot("pairwiseCipher")]
    [WattPilot("f", 0)]
    public WifiCipher? PairwiseCipher
    {
        get;
        set => Set(ref field, value);
    }

    [JsonProperty("groupCipher")]
    [WattPilot("groupCipher")]
    [WattPilot("f", 1)]
    public WifiCipher? GroupCipher
    {
        get;
        set => Set(ref field, value);
    }

    [JsonProperty("lr")]
    [WattPilot("lr")]
    [WattPilot("f", 5)]
    public bool? SupportsLowRate
    {
        get;
        set => Set(ref field, value);
    }

    [JsonProperty("wps")]
    [WattPilot("wps")]
    [WattPilot("f", 6)]
    public bool? AllowWps
    {
        get;
        set => Set(ref field, value);
    }

    [JsonProperty("ftmResponder")]
    [WattPilot("ftmResponder")]
    [WattPilot("f", 7)]
    public bool? IsFtmResponder
    {
        get;
        set => Set(ref field, value);
    }

    [JsonProperty("ftmInitiator")]
    [WattPilot("ftmInitiator")]
    [WattPilot("f", 8)]
    public bool? IsFtmInitiator
    {
        get;
        set => Set(ref field, value);
    }

    [JsonProperty("bssid")]
    [WattPilot("bssid")]
    public string? MacAddressString
    {
        get;
        set => Set(ref field, value);
    }

    [JsonProperty("rssi")]
    [WattPilot("rssi")]
    public int? WifiSignal
    {
        get;
        set => Set(ref field, value);
    }

    [JsonProperty("netmask")]
    [WattPilot("netmask")]
    public IPAddress? NetMask
    {
        get;
        set => Set(ref field, value);
    }

    [JsonProperty("gw")]
    [WattPilot("gw")]
    public IPAddress? Gateway
    {
        get;
        set => Set(ref field, value);
    }

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