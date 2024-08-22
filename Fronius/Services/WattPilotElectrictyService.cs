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

        public Task<IEnumerable<AwattarCountry>> GetSupportedPriceZones() => Task.FromResult<IEnumerable<AwattarCountry>>(Array.Empty<AwattarCountry>());
    }
}
