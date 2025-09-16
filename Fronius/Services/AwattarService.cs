using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace De.Hochstaetter.Fronius.Services;

public sealed partial class AwattarService : ElectricityPushPriceServiceBase, IElectricityPriceService, IDisposable
{
    private HttpClient? client = new();
    private Timer? timer;
    private readonly AwattarCountry[] supportedPriceZones = [AwattarCountry.Austria, AwattarCountry.GermanyLuxembourg];
    private readonly SettingsBase settings;
    // https://api.awattar.de/v1/prices?zipcode=85375&consumption=1800&gridoperator=9901068000001&tariff=hourly-2&domain=awattar&date=2024-11-25

    public AwattarService()
    {
        timer = new Timer(TimerCallback, null, DurationToNextQuery, TimeSpan.Zero);
        settings = IoC.Get<SettingsBase>();
    }

    [ObservableProperty]
    public partial ICollection<AwattarPriceComponent>? Prices { get; set; }

    public bool CanSetPriceRegion => true;
    public override bool SupportsHistoricData => true;

    public AwattarCountry PriceRegion
    {
        get;
        set => Set(ref field, value, () => _ = Refresh(), () => supportedPriceZones.Contains(value) ? value : throw new ArgumentException(Resources.UnsupportedPriceZone));
    } = (AwattarCountry)(-1);

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

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public override async Task<IEnumerable<IElectricityPrice>> GetHistoricPriceDataAsync(DateTime? from, DateTime? to, CancellationToken token = default)
    {


        var (tld, fromUnix, toUnix) = GetQueryParameters(from, to);
        var query = FormattableString.Invariant($"https://api.awattar.{tld}/v1/marketdata{fromUnix}{toUnix}");

        return
            (await (client?.GetFromJsonAsync<AwattarPriceList>(query, token).ConfigureAwait(false) ?? throw new ObjectDisposedException(GetType().Name)))?.Prices ?? [];
    }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public async Task<AwattarEnergyList> GetDataAsync(DateTime? from, DateTime? to, CancellationToken token = default)
    {
        var (tld, fromUnix, toUnix) = GetQueryParameters(from, to);
        var query = FormattableString.Invariant($"https://api.awattar.{tld}/v1/power/productions{fromUnix}{toUnix}");

        if (client != null && settings.Awattar != null && Prices == null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.Awattar.Bearer);
            var queryString = $"https://api.awattar.{tld}/v1/prices?zipcode={settings.Awattar.PostalCode}&consumption=1800&gridoperator={settings.Awattar.GridOperatorId}&tariff=hourly-2&domain=awattar&date={from ?? to:yyyy-MM-dd}";

            try
            {
                Prices = (await client.GetFromJsonAsync<AwattarPriceInfo>(queryString, token).ConfigureAwait(false))?.Data.Prices;

                if (Prices != null)
                {
                    Prices.Add(new AwattarPriceComponent { Description = "Aufschlag Awattar", Name = "Aufschlag Awattar", PriceUnit = "Cent/kWh", TaxRate = 0.19m, Price = 1.5m });
                    settings.ElectricityPrice.PriceOffsetBuy = Prices.Where(p => p.PriceUnit == "Cent/kWh").Sum(p => p.Price);
                }
            }
            catch
            {
                // We don't care
            }
        }


        var result = (await (client?.GetFromJsonAsync<AwattarEnergyList>(query, token).ConfigureAwait(false) ?? throw new ObjectDisposedException(GetType().Name))) ?? new();
        result.Prices = Prices;
        return result;
    }

    private (string tld, string fromUnix, string toUnix) GetQueryParameters(DateTime? from, DateTime? to)
    {
        var tld = PriceRegion switch
        {
            AwattarCountry.GermanyLuxembourg => "de",
            AwattarCountry.Austria => "at",
            _ => throw new NotSupportedException(Resources.UnsupportedPriceZone)
        };

        var fromUnix = from == null ? string.Empty : FormattableString.Invariant($"?start={GetUnixMilliseconds(from)}");
        var toUnix = to == null ? string.Empty : FormattableString.Invariant($"{(from == null ? "?" : "&")}end={GetUnixMilliseconds(to)}");
        return (tld, fromUnix, toUnix);
    }

    public void Dispose()
    {
        timer?.Dispose();
        timer = null;
        client?.Dispose();
        client = null;
    }

    ~AwattarService() => Dispose();

    private static long? GetUnixMilliseconds(DateTime? date) => date is null ? null : (long)Math.Round((date.Value.ToUniversalTime() - DateTime.UnixEpoch).TotalMilliseconds, MidpointRounding.AwayFromZero);

    private async Task Refresh(CancellationToken token = default)
    {
        if (!supportedPriceZones.Contains(PriceRegion))
        {
            throw new NotSupportedException(Resources.UnsupportedPriceZone);
        }

        RawValues = await GetHistoricPriceDataAsync(null, new DateTime(2064, 12, 25), token).ConfigureAwait(false);
    }

    private void TimerCallback(object? state)
    {
        timer?.Change(DurationToNextQuery, TimeSpan.Zero);
        _ = Refresh();
    }
}
