namespace De.Hochstaetter.Fronius.Models.Gen24
{
    public enum Gen24ConnectedInverterStatus : byte
    {
        [EnumParse(ParseAs = "online")] Online,
        [EnumParse(ParseAs = "offline")] Offline
    }

    public class Gen24ConnectedInverter : BindableBase, IHaveDisplayName
    {
        private bool useDevice;

        [FroniusProprietaryImport("activated", FroniusDataType.Root)]
        public bool UseDevice
        {
            get => useDevice;
            set => Set(ref useDevice, value);
        }

        private string dataSourceId = string.Empty;

        [FroniusProprietaryImport("dataSourceId", FroniusDataType.Root)]
        public string DataSourceId
        {
            get => dataSourceId;
            set => Set(ref dataSourceId, value);
        }

        private string deviceType = string.Empty;

        [FroniusProprietaryImport("deviceType", FroniusDataType.Root)]
        public string DeviceType
        {
            get => deviceType;
            set => Set(ref deviceType, value);
        }

        private string hostname = string.Empty;

        [FroniusProprietaryImport("hostname", FroniusDataType.Root)]
        public string Hostname
        {
            get => hostname;
            set => Set(ref hostname, value);
        }

        private string ipAddressString = string.Empty;

        [FroniusProprietaryImport("ipAddress", FroniusDataType.Root)]
        public string IpAddressString
        {
            get => ipAddressString;
            set => Set(ref ipAddressString, value, () => NotifyOfPropertyChange(nameof(IpAddress)));
        }

        public IPAddress? IpAddress
        {
            get => string.IsNullOrEmpty(IpAddressString) ? null : IPAddress.Parse(IpAddressString);
            set => IpAddressString = value?.ToString() ?? string.Empty;
        }

        private string displayName = string.Empty;

        [FroniusProprietaryImport("name", FroniusDataType.Root)]
        public string DisplayName
        {
            get => displayName;
            set => Set(ref displayName, value);
        }

        private string serialNumber = string.Empty;

        [FroniusProprietaryImport("serial", FroniusDataType.Root)]
        public string SerialNumber
        {
            get => serialNumber;
            set => Set(ref serialNumber, value);
        }

        private Gen24ConnectedInverterStatus status;

        [FroniusProprietaryImport("status", FroniusDataType.Root)]
        public Gen24ConnectedInverterStatus Status
        {
            get => status;
            set => Set(ref status, value);
        }

        private Guid id = Guid.NewGuid();

        public Guid Id
        {
            get => id;
            set => Set(ref id, value);
        }

        private bool isAutoDetected;

        public bool IsAutoDetected
        {
            get => isAutoDetected;
            set => Set(ref isAutoDetected, value);
        }
    }
}
