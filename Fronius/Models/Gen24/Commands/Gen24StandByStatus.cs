namespace De.Hochstaetter.Fronius.Models.Gen24.Commands;

public partial class Gen24StandByStatus : Gen24NoResultCommand
{
    [FroniusProprietaryImport("standby", FroniusDataType.Root)]
    [ObservableProperty]
    public partial bool IsStandBy { get; set; }

    [FroniusProprietaryImport("hasRequestHighestPriority", FroniusDataType.Root)]
    [ObservableProperty]
    public partial bool HasRequestHighestPriority { get; set; }
}