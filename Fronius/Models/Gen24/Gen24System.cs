namespace De.Hochstaetter.Fronius.Models.Gen24
{
    public partial class Gen24System : BindableBase, IHaveDisplayName, IHaveUniqueId
    {
        [ObservableProperty, NotifyPropertyChangedFor(nameof(DisplayName), nameof(Model), nameof(SerialNumber))]
        [NotifyPropertyChangedFor(nameof(StorageNetCapacity), nameof(MaxStorageNetCapacity), nameof(NetStateOfChange))]
        public partial Gen24Config? Config { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StorageNetCapacity), nameof(MaxStorageNetCapacity), nameof(NetStateOfChange))]
        public partial Gen24Sensors? Sensors { get; set; }

        [JsonIgnore]
        public IGen24Service Service { get; init; } = null!;

        public double? StorageNetCapacity => MaxStorageNetCapacity * NetStateOfChange;

        public double? MaxStorageNetCapacity => (1 - GetSocMin()) * Sensors?.Storage?.MaxCapacity;

        public double? NetStateOfChange => GetNetStateOfCharge(Sensors?.Storage?.StateOfCharge);

        [JsonIgnore]
        public string DisplayName => $"{Config?.Versions?.ModelName ?? "GEN24"} - {Config?.InverterSettings?.SystemName ?? "GEN24"} - {Config?.Versions?.SerialNumber ?? "---"}";

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

        private double GetSocMin()
        {
            var socMinConfig = Config?.BatterySettings?.SocMin ?? 0;
            var socMinPreserve = Config?.BatterySettings?.EnableSystemDeadlockPrevention is true ? Config?.BatterySettings?.SocMinPreserve ?? -1 : -1;
            
            var socMinManufacturer = Sensors?.Storage?.Manufacturer switch
            {
                "BYD" => 5d,
                _ => 0d,
            };
            
            return NumberExtensions.Max(Config?.BatterySettings?.BackupReserve ?? 0, socMinPreserve, Config?.BatterySettings?.Limits is SocLimits.UseManufacturerDefault or null ? socMinManufacturer : socMinConfig) / 100d;
        }

        private double? GetNetStateOfCharge(double? grossStateOfCharge)
        {
            var socMin = GetSocMin();
            return (grossStateOfCharge - socMin) / (1 - socMin);
        }
    }
}
