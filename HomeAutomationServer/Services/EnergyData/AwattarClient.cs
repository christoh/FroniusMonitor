using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace De.Hochstaetter.HomeAutomationServer.Services.EnergyData;

/// <summary>
///     Talks to <c>api.awattar.de</c> and <c>api.awattar.at</c>. The three endpoints are the ones the official
///     documentation describes and the ones the WPF app has used for years; only the wrapping changed.
/// </summary>
public sealed class AwattarClient(ILogger<AwattarClient> logger) : IAwattarClient, IDisposable
{
    /// <summary>How Awattar's answers are read; public so a test can read a recorded answer the same way.</summary>
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient client = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("HomeAutomationServer", FroniusGitInfo.Version.ToString()));
        return client;
    }

    public async Task<IReadOnlyList<EnergyPricePoint>> GetMarketPricesAsync(AwattarCountry region, DateTime fromUtc, DateTime toUtc, CancellationToken token = default)
    {
        var uri = $"{BaseUri(region)}/v1/marketdata{SpanQuery(fromUtc, toUtc)}";
        var list = await GetAsync<AwattarPriceList>(uri, null, token).ConfigureAwait(false);

        return list.Prices
            .Select(p => new EnergyPricePoint
            {
                StartTime = p.StartTime,
                EndTime = p.EndTime,
                CentsPerKiloWattHour = p.CentsPerKiloWattHour,
                Source = EnergyPriceSource.Awattar,
            })
            .OrderBy(p => p.StartTime)
            .ToList();
    }

    public async Task<IReadOnlyList<GridProductionPoint>> GetProductionsAsync(AwattarCountry region, DateTime fromUtc, DateTime toUtc, CancellationToken token = default)
    {
        var uri = $"{BaseUri(region)}/v1/power/productions{SpanQuery(fromUtc, toUtc)}";
        var list = await GetAsync<AwattarEnergyList>(uri, null, token).ConfigureAwait(false);
        return list.ToProductions();
    }

    public async Task<IReadOnlyList<EnergyPriceComponent>> GetPriceComponentsAsync(EnergyDataSettings settings, DateOnly day, CancellationToken token = default)
    {
        if (!settings.HasTariffQuery)
        {
            throw new InvalidOperationException("The tariff query needs the bearer, the postal code and the grid operator id");
        }

        // consumption and tariff are what the WPF app has always sent; the answer does not depend on the
        // consumption for the per kWh components, and hourly-2 is the dynamic tariff.
        var uri = FormattableString.Invariant(
            $"{BaseUri(settings.PriceRegion)}/v1/prices?zipcode={Uri.EscapeDataString(settings.PostalCode)}&consumption=1800&gridoperator={Uri.EscapeDataString(settings.GridOperatorId)}&tariff=hourly-2&domain=awattar&date={day:yyyy-MM-dd}");

        var info = await GetAsync<AwattarPriceInfo>(uri, settings.Bearer, token).ConfigureAwait(false);

        return info.Data.Prices
            .Where(c => !string.IsNullOrEmpty(c.Name))
            .Select(c => new EnergyPriceComponent
            {
                Name = c.Name!,
                Description = c.Description ?? c.Name!,
                NetPrice = c.Price,
                TaxRate = c.TaxRate,
                Unit = c.PriceUnit ?? EnergyPriceComponent.CentsPerKiloWattHourUnit,
            })
            .ToList();
    }

    private async Task<T> GetAsync<T>(string uri, string? bearer, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);

        if (bearer != null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        }

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Awattar request: {Uri}", uri);
        }

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, token).ConfigureAwait(false) ?? throw new InvalidDataException($"Awattar answered {uri} with no data");
    }

    private static string BaseUri(AwattarCountry region) => region switch
    {
        AwattarCountry.GermanyLuxembourg => "https://api.awattar.de",
        AwattarCountry.Austria => "https://api.awattar.at",
        _ => throw new NotSupportedException(Resources.UnsupportedPriceZone),
    };

    private static string SpanQuery(DateTime fromUtc, DateTime toUtc)
    {
        return FormattableString.Invariant($"?start={UnixMilliseconds(fromUtc)}&end={UnixMilliseconds(toUtc)}");
    }

    private static long UnixMilliseconds(DateTime utc) => (long)Math.Round((utc.ToUniversalTime() - DateTime.UnixEpoch).TotalMilliseconds, MidpointRounding.AwayFromZero);

    public void Dispose() => client.Dispose();
}
