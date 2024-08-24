namespace De.Hochstaetter.Fronius.Services
{
    public class WattPilotElectricityService : ElectricityPushPriceServiceBase, IElectricityPriceService
    {
        private AwattarCountry priceZone;

        public AwattarCountry PriceRegion
        {
            get => priceZone;
            set => Set(ref priceZone, value);
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
