namespace De.Hochstaetter.Fronius.Models.Gen24;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public abstract class Gen24DeviceBase : BindableBase, ICloneable
{
    public DateTime ReceivedTime { get; } = DateTime.UtcNow;

    public TimeSpan Latency => ReceivedTime - (DataTime ?? ReceivedTime);

    [FroniusProprietaryImport("COMPONENTS_TIME_STAMP_U64")]
    public DateTime? DataTime
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("manufacturer", FroniusDataType.Attribute)]
    public string? Manufacturer
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("model", FroniusDataType.Attribute)]
    public string? Model
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("[ENABLE]", FroniusDataType.Attribute)]
    public bool? IsEnabled
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("[VISIBLE]", FroniusDataType.Attribute)]
    public bool? IsVisible
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("addr", FroniusDataType.Attribute)]
    public ushort? ModbusAddress
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("id", FroniusDataType.Attribute)]
    public string? Id
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("if", FroniusDataType.Attribute)]
    public string? BusId
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("serial", FroniusDataType.Attribute)]
    public string? SerialNumber
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("createTS", FroniusDataType.Attribute)]
    public DateTime? CreationTime
    {
        get;
        set => Set(ref field, value);
    }

    public uint GroupId
    {
        get;
        set => Set(ref field, value);
    }

    public object Clone() => MemberwiseClone();
}