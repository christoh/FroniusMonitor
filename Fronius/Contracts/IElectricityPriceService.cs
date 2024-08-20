namespace De.Hochstaetter.Fronius.Contracts;

public interface IElectricityPriceService : INotifyPropertyChanged
{
    public Task<IEnumerable<IElectricityPrice>?> GetGetElectricityPricesAsync(string priceZoneCode, double offset = 0, double factor = 1, CancellationToken token = default);
    public Task<IEnumerable<string>> GetSupportedPriceZones();
    public bool IsPush { get; }
    public bool CanSetPriceZone { get; }
    public IEnumerable<WattPilotElectricityPrice>? RawValues { get; }
}
