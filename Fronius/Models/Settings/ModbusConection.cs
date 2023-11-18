namespace De.Hochstaetter.Fronius.Models.Settings
{
    public class ModbusConnection : BindableBase, IHaveDisplayName
    {
        private string hostName = "192.168.178.xxx";

        [XmlAttribute]
        public string HostName
        {
            get => hostName;
            set => Set(ref hostName, value);
        }

        private ushort port;

        [XmlAttribute]
        public ushort Port
        {
            get => port;
            set => Set(ref port, value);
        }

        private byte modbusAddress;

        [XmlAttribute]
        public byte ModbusAddress
        {
            get => modbusAddress;
            set => Set(ref modbusAddress, value);
        }

        [XmlIgnore] public string DisplayName => $"[{SurroundIpV6Address(hostName)}]:{Port}/{ModbusAddress}";

        public override string ToString() => DisplayName;

        private string SurroundIpV6Address(string hostNameOrAddress)
        {
            return IPAddress.TryParse(hostNameOrAddress, out var ipAddress) && ipAddress.AddressFamily == AddressFamily.InterNetworkV6 ? $"[{hostNameOrAddress}]" : hostNameOrAddress;
        }
    }
}
