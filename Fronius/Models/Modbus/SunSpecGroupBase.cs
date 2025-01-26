namespace De.Hochstaetter.Fronius.Models.Modbus;

public abstract class SunSpecGroupBase : IHaveDisplayName, IHaveUniqueId
{
    private readonly SunSpecCommonBlock common;

    protected SunSpecGroupBase(IEnumerable<SunSpecModelBase> models)
    {
        Models = models as IReadOnlyList<SunSpecModelBase> ?? models.ToArray();

        if (Models.OfType<SunSpecCommonBlock>().Count() != 1)
        {
            throw new InvalidDataException($"{nameof(models)} must contain exactly one {nameof(SunSpecCommonBlock)}");
        }

        common = Models.OfType<SunSpecCommonBlock>().First();
    }
    public IReadOnlyList<SunSpecModelBase> Models { get; }

    public bool IsPresent => true;
    public string? Manufacturer => common.Manufacturer;
    public string? Model => common.ModelName;
    //public string? ModelName => common.ModelName;
    public string? SerialNumber => common.SerialNumber;
    public string? Version => common.Version;
    public string? Options => common.Options;
    public string DisplayName => common.ToString();
    public override string ToString() => common.ToString();
}
