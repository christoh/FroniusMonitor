namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class WattPilotDetailsViewModel(IDataCollectionService dataCollectionService, IWattPilotService wattPilotService) : ViewModelBase
{
    public IDataCollectionService DataCollectionService => dataCollectionService;
    public IWattPilotService WattPilotService => wattPilotService;
}
