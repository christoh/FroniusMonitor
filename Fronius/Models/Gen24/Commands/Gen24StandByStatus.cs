namespace De.Hochstaetter.Fronius.Models.Gen24.Commands;

public class Gen24StandByStatus : Gen24NoResultCommand
{
    private bool isStandBy;

    [FroniusProprietaryImport("standby", FroniusDataType.Root)]
    public bool IsStandBy
    {
        get => isStandBy;
        set => Set(ref isStandBy, value);
    }

    private bool hasRequestHighestPriority;

    [FroniusProprietaryImport("hasRequestHighestPriority", FroniusDataType.Root)]
    public bool HasRequestHighestPriority
    {
        get => hasRequestHighestPriority;
        set => Set(ref hasRequestHighestPriority, value);
    }
}