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

    public virtual Task<IEnumerable<IElectricityPrice>> GetGetElectricityPricesAsync(decimal offset = 0, decimal factor = 1, CancellationToken token = default)
    {
        var result = RawValues?.Select(price =>
        {
            var newPrice = (IElectricityPrice)price.Clone();
            newPrice.CentsPerKiloWattHour *= factor;
            newPrice.CentsPerKiloWattHour += offset;
            return newPrice;
        }) ?? Array.Empty<AwattarElectricityPrice>();

        return Task.FromResult(result);
    }
}
