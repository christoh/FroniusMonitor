namespace De.Hochstaetter.Fronius.Contracts;

public interface IElectricityPriceService
{
    public Task<IEnumerable<IElectricityPrice>?> GetGetElectricityPricesAsync(string priceZoneCode, double offset = 0, double factor = 1, CancellationToken token = default);
    public Task<IEnumerable<string>> GetSupportedPriceZones();
}
