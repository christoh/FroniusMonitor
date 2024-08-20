using System.Net.Http.Json;

namespace De.Hochstaetter.Fronius.Services;

public sealed class AwattarService : ElectricityPushPriceServiceBase, IElectricityPriceService, IDisposable
{
    private HttpClient? client = new();
    private Timer? timer;
    private readonly AwattarCountry[] supportedPriceZones = [AwattarCountry.Austria, AwattarCountry.GermanyLuxembourg];

    public AwattarService()
    {
        var nextQuery = GetNextQuery();
        timer = new Timer(TimerCallback, null, nextQuery, TimeSpan.Zero);
        _ = Refresh();
    }

    public bool CanSetPriceZone => true;

    private AwattarCountry priceZone = AwattarCountry.GermanyLuxembourg;

    public AwattarCountry PriceZone
    {
        get => priceZone;
        set => Set(ref priceZone, value, preFunc: () => supportedPriceZones.Contains(value) ? value : throw new ArgumentException(Resources.UnsupportedPriceZone));
    }

    public Task<IEnumerable<AwattarCountry>> GetSupportedPriceZones() => Task.FromResult<IEnumerable<AwattarCountry>>(supportedPriceZones);

    public void Dispose()
    {
        client?.Dispose();
        client = null;
        timer?.Dispose();
        timer = null;
    }

    private async Task Refresh(CancellationToken token = default)
    {
        if (!supportedPriceZones.Contains(PriceZone))
        {
            throw new NotSupportedException(Resources.UnsupportedPriceZone);
        }

        var tld = PriceZone switch
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
        timer?.Change(GetNextQuery(), TimeSpan.Zero);
        _ = Refresh();
    }
    private static TimeSpan GetNextQuery()
    {
        return new DateTime(DateTime.UtcNow.Ticks / 36_000_000_000L * 36_000_000_000L).AddSeconds(3690) - DateTime.UtcNow;
    }
}