using System.Net.Http.Json;

namespace De.Hochstaetter.Fronius.Services;

public sealed class AwattarService : IElectricityPriceService, IDisposable
{
    private HttpClient? client = new();
    private readonly string[] supportedPriceZones = ["at", "de"];

    public async Task<IEnumerable<IElectricityPrice>?> GetGetElectricityPricesAsync(string priceZoneCode, double offset = 0, double factor = 1, CancellationToken token = default)
    {
        if (!supportedPriceZones.Contains(priceZoneCode))
        {
            throw new NotSupportedException(Resources.UnsupportedPriceZone);
        }

        // ReSharper disable once StringLiteralTypo
        var result = (await (client?.GetFromJsonAsync<AwattarPriceList>($"https://api.awattar.{priceZoneCode}/v1/marketdata", token).ConfigureAwait(false) ?? throw new ObjectDisposedException(GetType().Name)))?.Prices;

        if (result == null)
        {
            return null;
        }

        result.Apply(price =>
        {
            price.CentsPerKiloWattHour *= (decimal)factor;
            price.CentsPerKiloWattHour += (decimal)offset;
        });
        
        return result;
    }

    public Task<IEnumerable<string>> GetSupportedPriceZones() => Task.FromResult<IEnumerable<string>>(supportedPriceZones);

    public void Dispose()
    {
        client?.Dispose();
        client = null;
    }
}