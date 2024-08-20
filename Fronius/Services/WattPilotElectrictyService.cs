namespace De.Hochstaetter.Fronius.Services
{
    public class WattPilotElectricityService : ElectricityPushPriceServiceBase, IElectricityPriceService
    {
        private AwattarCountry priceZone;

        public AwattarCountry PriceZone
        {
            get => priceZone;
            set => Set(ref priceZone, value);
        }

        public bool CanSetPriceZone => false;

        public Task<IEnumerable<AwattarCountry>> GetSupportedPriceZones() => Task.FromResult<IEnumerable<AwattarCountry>>(Array.Empty<AwattarCountry>());
    }
}
