namespace De.Hochstaetter.Fronius.Models.Modbus;

public abstract class SunSpecGroupBase
{
    public IReadOnlyList<SunSpecModelBase> Models { get; }

    protected SunSpecGroupBase(IEnumerable<SunSpecModelBase> models)
    {
        Models = models as IReadOnlyList<SunSpecModelBase> ?? models.ToArray();

        if (Models.OfType<SunSpecCommonBlock>().Count() != 1)
        {
            throw new InvalidDataException($"{nameof(models)} must contain exactly one {nameof(SunSpecCommonBlock)}");
        }
    }
}
