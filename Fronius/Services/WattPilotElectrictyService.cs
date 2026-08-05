namespace De.Hochstaetter.Fronius.Services;

public partial class WattPilotElectricityService : ElectricityPushPriceServiceBase, IElectricityPriceService
{
    [ObservableProperty]
    public partial AwattarCountry PriceRegion { get; set; }

    public bool CanSetPriceRegion => false;
    public override bool SupportsHistoricData => false;

    public override Task<IEnumerable<IElectricityPrice>> GetHistoricPriceDataAsync(DateTime? from, DateTime? to, CancellationToken token = default)
    {
        throw new NotSupportedException($"{GetType().Name} does not support historic data");
    }

    public void UpdateData(WattPilotElectricityPrice priceInfo)
    {
        var result = new ElectricityPrice[priceInfo.CentsPerKiloWattHour.Length];

        for (int i = 0; i < priceInfo.CentsPerKiloWattHour.Length; i++)
        {
            result[i] = new ElectricityPrice
            {
                CentsPerKiloWattHour = priceInfo.CentsPerKiloWattHour[i],
                StartTime = priceInfo.StartTime.AddSeconds(priceInfo.IntervalSeconds * i),
                EndTime = priceInfo.StartTime.AddSeconds(priceInfo.IntervalSeconds * (i + 1)),
            };
        }

        RawValues = result;

        NotifyOfPropertyChange(nameof(RawValues));
    }

    public Task<IEnumerable<AwattarCountry>> GetSupportedPriceZones() => Task.FromResult<IEnumerable<AwattarCountry>>(Array.Empty<AwattarCountry>());
}