namespace De.Hochstaetter.Fronius.Contracts;

public interface IElectricityPriceService : INotifyPropertyChanged
{
    public Task<IEnumerable<IElectricityPrice>> GetGetElectricityPricesAsync(double offset = 0, double factor = 1, CancellationToken token = default);
    public Task<IEnumerable<AwattarCountry>> GetSupportedPriceZones();
    public bool IsPush { get; }
    public bool CanSetPriceZone { get; }
    public IEnumerable<IElectricityPrice>? RawValues { get; }
    public AwattarCountry PriceZone { get; set; }
}
