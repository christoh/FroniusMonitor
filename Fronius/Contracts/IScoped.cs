namespace De.Hochstaetter.Fronius.Contracts;

public interface IInverterScoped
{
    IGen24Service Gen24Service { get; }
}
