namespace De.Hochstaetter.Fronius.Models.Gen24.Commands;

public partial class Gen24SystemPreservation : Gen24NoResultCommand
{
    [FroniusProprietaryImport("socMinValue", FroniusDataType.Root)]
    [ObservableProperty]
    public partial double MinSoc { get; set; }
}