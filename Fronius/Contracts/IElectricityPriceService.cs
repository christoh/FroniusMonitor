namespace De.Hochstaetter.Fronius.Contracts;

public interface IElectricityPriceService : INotifyPropertyChanged
{
    public Task<IEnumerable<IElectricityPrice>> GetGetElectricityPricesAsync(decimal offset = 0, decimal factor = 1, CancellationToken token = default);
    public Task<IEnumerable<AwattarCountry>> GetSupportedPriceZones();
    public bool IsPush { get; }
    public bool CanSetPriceRegion { get; }
    public IEnumerable<IElectricityPrice>? RawValues { get; }
    public AwattarCountry PriceRegion { get; set; }
}
