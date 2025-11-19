namespace De.Hochstaetter.Fronius.Models.Gen24;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public abstract partial class Gen24DeviceBase : BindableBase, ICloneable
{
    public DateTime ReceivedTime { get; } = DateTime.UtcNow;

    public TimeSpan Latency => ReceivedTime - (DataTime ?? ReceivedTime);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Latency))]
    [FroniusProprietaryImport("COMPONENTS_TIME_STAMP_U64")]
    public partial DateTime? DataTime { get; set ; }

    [ObservableProperty]
    [FroniusProprietaryImport("manufacturer", FroniusDataType.Attribute)]
    public partial string? Manufacturer { get; set ; }

    [ObservableProperty]
    [FroniusProprietaryImport("model", FroniusDataType.Attribute)]
    public partial string? Model { get; set ; }

    [ObservableProperty]
    [FroniusProprietaryImport("[ENABLE]", FroniusDataType.Attribute)]
    public partial bool? IsEnabled { get; set ; }

    [ObservableProperty]
    [FroniusProprietaryImport("[VISIBLE]", FroniusDataType.Attribute)]
    public partial bool? IsVisible { get; set ; }

    [ObservableProperty]
    [FroniusProprietaryImport("addr", FroniusDataType.Attribute)]
    public partial ushort? ModbusAddress { get; set ; }

    [ObservableProperty]
    [FroniusProprietaryImport("id", FroniusDataType.Attribute)]
    public partial string? Id { get; set ; }

    [ObservableProperty]
    [FroniusProprietaryImport("if", FroniusDataType.Attribute)]
    public partial string? BusId { get; set ; }

    [ObservableProperty]
    [FroniusProprietaryImport("serial", FroniusDataType.Attribute)]
    public partial string? SerialNumber { get; set ; }

    [ObservableProperty]
    [FroniusProprietaryImport("createTS", FroniusDataType.Attribute)]
    public partial DateTime? CreationTime { get; set ; }

    [ObservableProperty]
    public partial string? GroupId { get; set ; }

    public object Clone() => MemberwiseClone();
}