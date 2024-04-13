namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class WattPilotDetailsViewModel(IDataCollectionService dataCollectionService) : ViewModelBase
{
    public IDataCollectionService DataCollectionService => dataCollectionService;
}
