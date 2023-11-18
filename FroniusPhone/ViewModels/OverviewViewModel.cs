namespace FroniusPhone.ViewModels;

public class OverviewViewModel(IDataCollectionService dataCollectionService, Settings settings) : ViewModelBase
{
    public IDataCollectionService DataCollectionService
    {
        get => dataCollectionService;
        set => Set(ref dataCollectionService, value);
    }

    public Settings Settings
    {
        get => settings;
        set => Set(ref settings, value);
    }
}