namespace De.Hochstaetter.Fronius.Services;

public abstract class ElectricityPushPriceServiceBase : BindableBase
{
    public bool IsPush => true;

    private IEnumerable<IElectricityPrice>? rawValues;

    public virtual IEnumerable<IElectricityPrice>? RawValues
    {
        get => rawValues;
        set => Set(ref rawValues, value);
    }

    public abstract bool SupportsHistoricData { get; }

    public abstract Task<IEnumerable<IElectricityPrice>> GetHistoricPriceDataAsync(DateTime? from, DateTime? to, CancellationToken token = default);

    public virtual async Task<IEnumerable<IElectricityPrice>> GetElectricityPricesAsync(decimal offset = 0, decimal factor = 1, decimal vatFactor = 1, DateTime? from = null, DateTime? to = null, CancellationToken token = default)
    {
        if (!SupportsHistoricData && (from.HasValue || to.HasValue))
        {
            throw new NotSupportedException($"{GetType().Name} does not support historic data");
        }

        var result = (from.HasValue || to.HasValue ? await GetHistoricPriceDataAsync(from, to, token).ConfigureAwait(false) : RawValues)?.Select(price =>
        {
            var newPrice = (IElectricityPrice)price.Clone();
            newPrice.CentsPerKiloWattHour += Math.Abs(price.CentsPerKiloWattHour * (1 - factor)) + offset;
            newPrice.CentsPerKiloWattHour *= vatFactor;
            return newPrice;
        }) ?? Array.Empty<AwattarElectricityPrice>();

        return result;
    }
}
