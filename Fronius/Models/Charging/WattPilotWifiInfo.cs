using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using De.Hochstaetter.Fronius.Models.Wifi;
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
        public bool? IsB
        {
            get => isB;
            set => Set(ref isB, value, () => NotifyOfPropertyChange(nameof(Type)));
        }

        private bool? isG;
        [WattPilot("g")]
        public bool? IsG
        {
            get => isG;
            set => Set(ref isG, value, () => NotifyOfPropertyChange(nameof(Type)));
        }

        private bool? isN;
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
        public WifiCipher? PairwiseCipher
        {
            get => pairwiseCipher;
            set => Set(ref pairwiseCipher, value);
        }

        private WifiCipher? groupCipher;
        [WattPilot("groupCipher")]
        public WifiCipher? GroupCipher
        {
            get => groupCipher;
            set => Set(ref groupCipher, value);
        }

        private bool? supportsLowRate;
        [WattPilot("lr")]
        public bool? SupportsLowRate
        {
            get => supportsLowRate;
            set => Set(ref supportsLowRate, value);
        }

        private bool? allowWps;
        [WattPilot("wps")]
        public bool? AllowWps
        {
            get => allowWps;
            set => Set(ref allowWps, value);
        }

        private bool? isFtmResponder;
        [WattPilot("ftmResponder")]
        public bool? IsFtmResponder
        {
            get => isFtmResponder;
            set => Set(ref isFtmResponder, value);
        }

        private bool? isFtmInitiator;
        [WattPilot("ftmInitiator")]
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
