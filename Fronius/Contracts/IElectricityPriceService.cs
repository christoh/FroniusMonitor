namespace De.Hochstaetter.Fronius.Contracts;

public interface IElectricityPriceService : INotifyPropertyChanged
{
    public Task<IEnumerable<IElectricityPrice>> GetHistoricPriceDataAsync(DateTime? from, DateTime? to, CancellationToken token = default);
    public Task<IEnumerable<IElectricityPrice>> GetElectricityPricesAsync(decimal offset = 0, decimal factor = 1, decimal vatFactor = 1, DateTime? from = null, DateTime? to = null, CancellationToken token = default);
    public Task<IEnumerable<AwattarCountry>> GetSupportedPriceZones();
    public bool IsPush { get; }
    public bool CanSetPriceRegion { get; }
    public bool SupportsHistoricData { get; }
    public IEnumerable<IElectricityPrice>? RawValues { get; }
    public AwattarCountry PriceRegion { get; set; }
}
