namespace De.Hochstaetter.Fronius.Models.Gen24;

public enum Gen24ConnectedInverterStatus : byte
{
    [EnumParse(ParseAs = "online")] Online,
    [EnumParse(ParseAs = "offline")] Offline
}

public class Gen24ConnectedInverter : BindableBase, IHaveDisplayName
{
    [FroniusProprietaryImport("activated", FroniusDataType.Root)]
    public bool UseDevice
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("dataSourceId", FroniusDataType.Root)]
    public string DataSourceId
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [FroniusProprietaryImport("deviceType", FroniusDataType.Root)]
    public string DeviceType
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [FroniusProprietaryImport("modbusId", FroniusDataType.Root)]
    public byte ModbusAddress
    {
        get;
        set => Set(ref field, value);
    } = 1;

    [FroniusProprietaryImport("hostname", FroniusDataType.Root)]
    public string Hostname
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [FroniusProprietaryImport("ipAddress", FroniusDataType.Root)]
    public string IpAddressString
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(IpAddress)));
    } = string.Empty;

    public IPAddress? IpAddress
    {
        get => string.IsNullOrEmpty(IpAddressString) ? null : IPAddress.Parse(IpAddressString);
        set => IpAddressString = value?.ToString() ?? string.Empty;
    }

    [FroniusProprietaryImport("name", FroniusDataType.Root)]
    public string DisplayName
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [FroniusProprietaryImport("serial", FroniusDataType.Root)]
    public string SerialNumber
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [FroniusProprietaryImport("status", FroniusDataType.Root)]
    public Gen24ConnectedInverterStatus Status
    {
        get;
        set => Set(ref field, value);
    }

    public Guid Id
    {
        get;
        set => Set(ref field, value);
    } = Guid.NewGuid();

    public bool IsAutoDetected
    {
        get;
        set => Set(ref field, value);
    }

    public Gen24ConnectedInverter Copy()
    {
            return (Gen24ConnectedInverter)MemberwiseClone();
        }
}