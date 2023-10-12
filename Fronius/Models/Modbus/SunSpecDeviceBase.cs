namespace De.Hochstaetter.Fronius.Models.Modbus;

public abstract class SunSpecDeviceBase : BindableBase
{
    private string? manufacturer;

    public string? Manufacturer
    {
        get => manufacturer;
        set => Set(ref manufacturer, value);
    }

    private string? modelName;

    public string? ModelName
    {
        get => modelName;
        set => Set(ref modelName, value);
    }

    private string? dataManagerVersion;

    public string? DataManagerVersion
    {
        get => dataManagerVersion;
        set => Set(ref dataManagerVersion, value);
    }

    private string? deviceVersion;

    public string? DeviceVersion
    {
        get => deviceVersion;
        set => Set(ref deviceVersion, value);
    }

    private string? serialNumber;

    public string? SerialNumber
    {
        get => serialNumber;
        set => Set(ref serialNumber, value);
    }

    private ushort? modbusAddress;

    public ushort? ModbusAddress
    {
        get => modbusAddress;
        set => Set(ref modbusAddress, value);
    }

    public override string ToString() => $"{Manufacturer ?? "---"} - {ModelName ?? "---"} - {(object?)ModbusAddress ?? "---"}";
}