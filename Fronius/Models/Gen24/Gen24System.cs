namespace De.Hochstaetter.Fronius.Models.Gen24;

public partial class Gen24System : BindableBase, IHaveDisplayName, IHaveUniqueId
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(DisplayName), nameof(Model), nameof(SerialNumber))]
    [NotifyPropertyChangedFor(nameof(StorageNetCapacity), nameof(MaxStorageNetCapacity), nameof(NetStateOfChange), nameof(NetStateOfChangeIfFull))]
    public partial Gen24Config? Config { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StorageNetCapacity), nameof(MaxStorageNetCapacity), nameof(NetStateOfChange), nameof(NetStateOfChangeIfFull))]
    public partial Gen24Sensors? Sensors { get; set; }

    [JsonIgnore] public IGen24Service Service { get; init; } = null!;

    public double? StorageNetCapacity => Sensors?.Storage?.MaxCapacity * NetStateOfChangeIfFull;

    public double? MaxStorageNetCapacity => Sensors?.Storage?.MaxCapacity * GetNetStateOfChargeIfFull((Config?.BatterySettings?.Limits is SocLimits.UseManufacturerDefault or null ? 100 : Config?.BatterySettings?.SocMax ?? 100) / 100d);

    public double? NetStateOfChange => StorageNetCapacity / MaxStorageNetCapacity;

    public double? NetStateOfChangeIfFull => GetNetStateOfChargeIfFull(Sensors?.Storage?.StateOfCharge);

    [JsonIgnore] public string DisplayName => $"{Config?.Versions?.ModelName ?? "GEN24"} - {Config?.InverterSettings?.SystemName ?? "GEN24"} - {Config?.Versions?.SerialNumber ?? "---"}";

    public override string ToString() => DisplayName;

    public bool IsPresent => true;
    public string Manufacturer => "Fronius";
    public string? Model => Config?.Versions?.ModelName;
    public string? SerialNumber => Config?.Versions?.SerialNumber;
    
    public void CopyFrom(Gen24System other)
    {
        ArgumentNullException.ThrowIfNull(other, nameof(other));
        Config = other.Config;
        Sensors = other.Sensors;
        Refresh();
    }

    private double? GetNetStateOfChargeIfFull(double? stateOfCharge)
    {
        var socMin = Config?.BatterySettings?.SocMin ?? 0;
        var socMinPreserve = Config?.BatterySettings?.EnableSystemDeadlockPrevention is true ? Config?.BatterySettings?.SocMinPreserve ?? -1 : -1;
        return stateOfCharge - Math.Max(Config?.BatterySettings?.BackupReserve ?? 0, Math.Max(socMinPreserve, Config?.BatterySettings?.Limits is SocLimits.UseManufacturerDefault or null ? 5 : socMin)) / 100d;
    }
}
