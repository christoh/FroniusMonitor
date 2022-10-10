// ReSharper disable StringLiteralTypo

namespace De.Hochstaetter.Fronius.Models.Charging
{
    public class WattPilotWifiInfo : BindableBase, IHaveDisplayName
    {
        private string? ssid;

        [WattPilot("ssid")]
        public string? Ssid
        {
            get => ssid;
            set => Set(ref ssid, value, () => NotifyOfPropertyChange(DisplayName));
        }

        private bool? isB;

        [WattPilot("b")]
        [WattPilot("f", 2)]
        public bool? IsB
        {
            get => isB;
            set => Set(ref isB, value, () => NotifyOfPropertyChange(nameof(Type)));
        }

        private bool? isG;

        [WattPilot("f", 3)]
        [WattPilot("g")]
        public bool? IsG
        {
            get => isG;
            set => Set(ref isG, value, () => NotifyOfPropertyChange(nameof(Type)));
        }

        private bool? isN;

        [WattPilot("f", 4)]
        [WattPilot("n")]
        public bool? IsN
        {
            get => isN;
            set => Set(ref isN, value, () => NotifyOfPropertyChange(nameof(Type)));
        }

        private IPAddress? ipV4Address;

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

        [WattPilot("channel")]
        public int? Channel
        {
            get => channel;
            set => Set(ref channel, value);
        }

        private WifiEncryption? encryption;

        [WattPilot("encryptionType")]
        public WifiEncryption? Encryption
        {
            get => encryption;
            set => Set(ref encryption, value);
        }

        private WifiCipher? pairwiseCipher;

        [WattPilot("pairwiseCipher")]
        [WattPilot("f", 0)]
        public WifiCipher? PairwiseCipher
        {
            get => pairwiseCipher;
            set => Set(ref pairwiseCipher, value);
        }

        private WifiCipher? groupCipher;

        [WattPilot("groupCipher")]
        [WattPilot("f", 1)]
        public WifiCipher? GroupCipher
        {
            get => groupCipher;
            set => Set(ref groupCipher, value);
        }

        private bool? supportsLowRate;

        [WattPilot("lr")]
        [WattPilot("f", 5)]
        public bool? SupportsLowRate
        {
            get => supportsLowRate;
            set => Set(ref supportsLowRate, value);
        }

        private bool? allowWps;

        [WattPilot("wps")]
        [WattPilot("f", 6)]
        public bool? AllowWps
        {
            get => allowWps;
            set => Set(ref allowWps, value);
        }

        private bool? isFtmResponder;

        [WattPilot("ftmResponder")]
        [WattPilot("f", 7)]
        public bool? IsFtmResponder
        {
            get => isFtmResponder;
            set => Set(ref isFtmResponder, value);
        }

        private bool? isFtmInitiator;

        [WattPilot("ftmInitiator")]
        [WattPilot("f", 8)]
        public bool? IsFtmInitiator
        {
            get => isFtmInitiator;
            set => Set(ref isFtmInitiator, value);
        }

        private string? macAddressString;

        [WattPilot("bssid")]
        public string? MacAddressString
        {
            get => macAddressString;
            set => Set(ref macAddressString, value);
        }

        private int? wifiSignal;

        [WattPilot("rssi")]
        public int? WifiSignal
        {
            get => wifiSignal;
            set => Set(ref wifiSignal, value);
        }

        private IPAddress? netMask;

        [WattPilot("netmask")]
        public IPAddress? NetMask
        {
            get => netMask;
            set => Set(ref netMask, value);
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
