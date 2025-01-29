namespace De.Hochstaetter.Fronius.Models.Gen24.Commands;

public class Gen24StandByStatus : Gen24NoResultCommand
{
    [FroniusProprietaryImport("standby", FroniusDataType.Root)]
    public bool IsStandBy
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("hasRequestHighestPriority", FroniusDataType.Root)]
    public bool HasRequestHighestPriority
    {
        get;
        set => Set(ref field, value);
    }
}