namespace De.Hochstaetter.Fronius.Services
{
    public class WattPilotElectricityService : BindableBase, IElectricityPriceService
    {
        private IEnumerable<WattPilotElectricityPrice>? rawValues;

        public IEnumerable<WattPilotElectricityPrice>? RawValues
        {
            get => rawValues;
            set => Set(ref rawValues, value);
        }

        public bool IsPush => true;
        public bool CanSetPriceZone => false;

        public Task<IEnumerable<IElectricityPrice>?> GetGetElectricityPricesAsync(string priceZoneCode, double offset = 0, double factor = 1, CancellationToken token = default)
        {
            if (RawValues == null)
            {
                return Task.FromResult<IEnumerable<IElectricityPrice>?>(null);
            }

            var result = RawValues.Select(p =>
            {
                var newPrice = (WattPilotElectricityPrice)p.Clone();
                newPrice.CentsPerKiloWattHour *= (decimal)factor;
                newPrice.CentsPerKiloWattHour += (decimal)offset;
                return newPrice;
            }).ToArray();

            return Task.FromResult<IEnumerable<IElectricityPrice>?>(result);
        }

        public Task<IEnumerable<string>> GetSupportedPriceZones() => Task.FromResult<IEnumerable<string>>(["**"]);
    }
}
