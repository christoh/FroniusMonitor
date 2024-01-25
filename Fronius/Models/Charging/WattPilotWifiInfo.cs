// ReSharper disable StringLiteralTypo

using Newtonsoft.Json;

namespace De.Hochstaetter.Fronius.Models.Charging
{
    public class WattPilotWifiInfo : BindableBase, IHaveDisplayName
    {
        private string? ssid;

        [JsonProperty("ssid")]
        [WattPilot("ssid")]
        public string? Ssid
        {
            get => ssid;
            set => Set(ref ssid, value, () => NotifyOfPropertyChange(DisplayName));
        }

        private bool? isB;

        [JsonProperty("b")]
        [WattPilot("b")]
        [WattPilot("f", 2)]
        public bool? IsB
        {
            get => isB;
            set => Set(ref isB, value, () => NotifyOfPropertyChange(nameof(Type)));
        }

        private bool? isG;

        [JsonProperty("g")]
        [WattPilot("f", 3)]
        [WattPilot("g")]
        public bool? IsG
        {
            get => isG;
            set => Set(ref isG, value, () => NotifyOfPropertyChange(nameof(Type)));
        }

        private bool? isN;

        [WattPilot("f", 4)]
        [JsonProperty("n")]
        [WattPilot("n")]
        public bool? IsN
        {
            get => isN;
            set => Set(ref isN, value, () => NotifyOfPropertyChange(nameof(Type)));
        }

        private IPAddress? ipV4Address;

        [JsonProperty("ip")]
        [WattPilot("ip")]
        public IPAddress? IpV4Address
        {
            get => ipV4Address;
            set => Set(ref ipV4Address, value);
        }

        private string? countryCode;

        [WattPilot("f", 9)]
        public string? CountryCode
        {
            get => countryCode;
            set => Set(ref countryCode, value);
        }

        private int? channel;

        [JsonProperty("channel")]
        [WattPilot("channel")]
        public int? Channel
        {
            get => channel;
            set => Set(ref channel, value);
        }

        private WifiEncryption? encryption;

        [JsonProperty("encryptionType")]
        [WattPilot("encryptionType")]
        public WifiEncryption? Encryption
        {
            get => encryption;
            set => Set(ref encryption, value);
        }

        private WifiCipher? pairwiseCipher;

        [JsonProperty("pairwiseCipher")]
        [WattPilot("pairwiseCipher")]
        [WattPilot("f", 0)]
        public WifiCipher? PairwiseCipher
        {
            get => pairwiseCipher;
            set => Set(ref pairwiseCipher, value);
        }

        private WifiCipher? groupCipher;

        [JsonProperty("groupCipher")]
        [WattPilot("groupCipher")]
        [WattPilot("f", 1)]
        public WifiCipher? GroupCipher
        {
            get => groupCipher;
            set => Set(ref groupCipher, value);
        }

        private bool? supportsLowRate;

        [JsonProperty("lr")]
        [WattPilot("lr")]
        [WattPilot("f", 5)]
        public bool? SupportsLowRate
        {
            get => supportsLowRate;
            set => Set(ref supportsLowRate, value);
        }

        private bool? allowWps;

        [JsonProperty("wps")]
        [WattPilot("wps")]
        [WattPilot("f", 6)]
        public bool? AllowWps
        {
            get => allowWps;
            set => Set(ref allowWps, value);
        }

        private bool? isFtmResponder;

        [JsonProperty("ftmResponder")]
        [WattPilot("ftmResponder")]
        [WattPilot("f", 7)]
        public bool? IsFtmResponder
        {
            get => isFtmResponder;
            set => Set(ref isFtmResponder, value);
        }

        private bool? isFtmInitiator;

        [JsonProperty("ftmInitiator")]
        [WattPilot("ftmInitiator")]
        [WattPilot("f", 8)]
        public bool? IsFtmInitiator
        {
            get => isFtmInitiator;
            set => Set(ref isFtmInitiator, value);
        }

        private string? macAddressString;

        [JsonProperty("bssid")]
        [WattPilot("bssid")]
        public string? MacAddressString
        {
            get => macAddressString;
            set => Set(ref macAddressString, value);
        }

        private int? wifiSignal;

        [JsonProperty("rssi")]
        [WattPilot("rssi")]
        public int? WifiSignal
        {
            get => wifiSignal;
            set => Set(ref wifiSignal, value);
        }

        private IPAddress? netMask;

        [JsonProperty("netmask")]
        [WattPilot("netmask")]
        public IPAddress? NetMask
        {
            get => netMask;
            set => Set(ref netMask, value);
        }

        private IPAddress? gateway;
        [JsonProperty("gw")]
        [WattPilot("gw")]
        public IPAddress? Gateway
        {
            get => gateway;
            set => Set(ref gateway, value);
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
}
