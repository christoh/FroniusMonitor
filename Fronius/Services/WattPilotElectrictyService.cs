namespace De.Hochstaetter.Fronius.Services
{
    public class WattPilotElectricityService : ElectricityPushPriceServiceBase, IElectricityPriceService
    {
        public AwattarCountry PriceRegion
        {
            get;
            set => Set(ref field, value);
        }

        public bool CanSetPriceRegion => false;
        public override bool SupportsHistoricData => false;

        public override Task<IEnumerable<IElectricityPrice>> GetHistoricPriceDataAsync(DateTime? from, DateTime? to, CancellationToken token = default)
        {
            throw new NotSupportedException($"{GetType().Name} does not support historic data");
        }

        public Task<IEnumerable<AwattarCountry>> GetSupportedPriceZones() => Task.FromResult<IEnumerable<AwattarCountry>>(Array.Empty<AwattarCountry>());
    }
}
