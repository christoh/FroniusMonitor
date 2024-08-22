using System.Net.Http.Json;

namespace De.Hochstaetter.Fronius.Services;

public sealed class AwattarService : ElectricityPushPriceServiceBase, IElectricityPriceService, IDisposable
{
    private HttpClient? client = new();
    private Timer? timer;
    private readonly AwattarCountry[] supportedPriceZones = [AwattarCountry.Austria, AwattarCountry.GermanyLuxembourg];

    public AwattarService()
    {
        timer = new Timer(TimerCallback, null, DurationToNextQuery, TimeSpan.Zero);
    }

    public bool CanSetPriceRegion => true;

    private AwattarCountry priceZone = (AwattarCountry)(-1);

    public AwattarCountry PriceRegion
    {
        get => priceZone;
        set => Set(ref priceZone, value, () => _ = Refresh(), () => supportedPriceZones.Contains(value) ? value : throw new ArgumentException(Resources.UnsupportedPriceZone));
    }

    private static TimeSpan DurationToNextQuery
    {
        get
        {
            const long ticksPerHour = 36_000_000_000L;

            // Full hour plus 90 seconds
            return new DateTime(DateTime.UtcNow.Ticks / ticksPerHour * ticksPerHour).AddSeconds(3690) - DateTime.UtcNow;
        }
    }

    public Task<IEnumerable<AwattarCountry>> GetSupportedPriceZones() => Task.FromResult<IEnumerable<AwattarCountry>>(supportedPriceZones);

    public void Dispose()
    {
        timer?.Dispose();
        timer = null;
        client?.Dispose();
        client = null;
    }

    private async Task Refresh(CancellationToken token = default)
    {
        if (!supportedPriceZones.Contains(PriceRegion))
        {
            throw new NotSupportedException(Resources.UnsupportedPriceZone);
        }

        var tld = PriceRegion switch
        {
            AwattarCountry.GermanyLuxembourg => "de",
            AwattarCountry.Austria => "at",
            _ => throw new NotSupportedException(Resources.UnsupportedPriceZone)
        };

        // ReSharper disable once StringLiteralTypo
        RawValues = (await (client?.GetFromJsonAsync<AwattarPriceList>($"https://api.awattar.{tld}/v1/marketdata", token).ConfigureAwait(false) ?? throw new ObjectDisposedException(GetType().Name)))?.Prices;
    }

    private void TimerCallback(object? state)
    {
        timer?.Change(DurationToNextQuery, TimeSpan.Zero);
        _ = Refresh();
    }
}