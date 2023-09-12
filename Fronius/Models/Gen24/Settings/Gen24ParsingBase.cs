namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

public abstract class Gen24ParsingBase : BindableBase, ICloneable
{
    protected static readonly IGen24JsonService Gen24JsonService = IoC.TryGet<IGen24JsonService>()!;

    public abstract object Clone();
}