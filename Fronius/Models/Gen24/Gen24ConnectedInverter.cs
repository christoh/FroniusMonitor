namespace De.Hochstaetter.Fronius.Models.Gen24;

public enum Gen24ConnectedInverterStatus : byte
{
    [EnumParse(ParseAs = "online")] Online,
    [EnumParse(ParseAs = "offline")] Offline
}

public partial class Gen24ConnectedInverter : BindableBase, IHaveDisplayName
{
    [ObservableProperty]
    [FroniusProprietaryImport("activated", FroniusDataType.Root)]
    public partial bool UseDevice { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("dataSourceId", FroniusDataType.Root)]
    public partial string DataSourceId { get; set; } = string.Empty;

    [ObservableProperty]
    [FroniusProprietaryImport("deviceType", FroniusDataType.Root)]
    public partial string DeviceType { get; set; } = string.Empty;

    [ObservableProperty]
    [FroniusProprietaryImport("modbusId", FroniusDataType.Root)]
    public partial byte ModbusAddress { get; set; } = 1;

    [ObservableProperty]
    [FroniusProprietaryImport("hostname", FroniusDataType.Root)]
    public partial string Hostname { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IpAddress))]
    [FroniusProprietaryImport("ipAddress", FroniusDataType.Root)]
    public partial string IpAddressString { get; set; } = string.Empty;

    public IPAddress? IpAddress
    {
        get => string.IsNullOrEmpty(IpAddressString) ? null : IPAddress.Parse(IpAddressString);
        set => IpAddressString = value?.ToString() ?? string.Empty;
    }

    [ObservableProperty]
    [FroniusProprietaryImport("name", FroniusDataType.Root)]
    public partial string DisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    [FroniusProprietaryImport("serial", FroniusDataType.Root)]
    public partial string SerialNumber { get; set; } = string.Empty;

    [ObservableProperty]
    [FroniusProprietaryImport("status", FroniusDataType.Root)]
    public partial Gen24ConnectedInverterStatus Status { get; set; }

    [ObservableProperty]
    public partial Guid Id { get; set; } = Guid.NewGuid();

    [ObservableProperty]
    public partial bool IsAutoDetected { get; set; }

    public Gen24ConnectedInverter Copy()
    {
        return (Gen24ConnectedInverter)MemberwiseClone();
    }
}