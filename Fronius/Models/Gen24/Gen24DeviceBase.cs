using System.Diagnostics.CodeAnalysis;
using De.Hochstaetter.Fronius.Attributes;

namespace De.Hochstaetter.Fronius.Models.Gen24;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public abstract class Gen24DeviceBase : BindableBase
{
    private DateTime? dataTime;

    [FroniusProprietaryImport("COMPONENTS_TIME_STAMP_U64")]
    public DateTime? DataTime
    {
        get => dataTime;
        set => Set(ref dataTime, value);
    }

    private string? manufacturer;

    [FroniusProprietaryImport("manufacturer", FroniusDataType.Attribute)]
    public string? Manufacturer
    {
        get => manufacturer;
        set => Set(ref manufacturer, value);
    }

    private string? model;

    [FroniusProprietaryImport("model", FroniusDataType.Attribute)]
    public string? Model
    {
        get => model;
        set => Set(ref model, value);
    }

    private bool? isEnabled;

    [FroniusProprietaryImport("[ENABLE]", FroniusDataType.Attribute)]
    public bool? IsEnabled
    {
        get => isEnabled;
        set => Set(ref isEnabled, value);
    }

    private bool? isVisible;

    [FroniusProprietaryImport("[VISIBLE]", FroniusDataType.Attribute)]
    public bool? IsVisible
    {
        get => isVisible;
        set => Set(ref isVisible, value);
    }

    private ushort? modbusAddress;
    [FroniusProprietaryImport("addr", FroniusDataType.Attribute)]
    public ushort? ModbusAddress
    {
        get => modbusAddress;
        set => Set(ref modbusAddress, value);
    }

    private string? id;
    [FroniusProprietaryImport("id", FroniusDataType.Attribute)]
    public string? Id
    {
        get => id;
        set => Set(ref id, value);
    }

    private string? busId;
    [FroniusProprietaryImport("if",FroniusDataType.Attribute)]
    public string? BusId
    {
        get => busId;
        set => Set(ref busId, value);
    }

    private string? serialNumber;

    [FroniusProprietaryImport("serial", FroniusDataType.Attribute)]
    public string? SerialNumber
    {
        get => serialNumber;
        set => Set(ref serialNumber, value);
    }

    private DateTime? creationTime;

    [FroniusProprietaryImport("createTS", FroniusDataType.Attribute)]
    public DateTime? CreationTime
    {
        get => creationTime;
        set => Set(ref creationTime, value);
    }
}