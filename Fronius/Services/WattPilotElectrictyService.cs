namespace De.Hochstaetter.Fronius.Services;

public partial class WattPilotElectricityService : ElectricityPushPriceServiceBase, IElectricityPriceService
{
    [ObservableProperty]
    public partial AwattarCountry PriceRegion { get; set; }

    public bool CanSetPriceRegion => false;
    public override bool SupportsHistoricData => false;

    public override Task<IEnumerable<IElectricityPrice>> GetHistoricPriceDataAsync(DateTime? from, DateTime? to, CancellationToken token = default)
    {
        throw new NotSupportedException($"{GetType().Name} does not support historic data");
    }

    public Task<IEnumerable<AwattarCountry>> GetSupportedPriceZones() => Task.FromResult<IEnumerable<AwattarCountry>>(Array.Empty<AwattarCountry>());
}