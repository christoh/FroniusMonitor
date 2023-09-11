namespace FroniusPhone.ViewModels;

public class OverviewViewModel : ViewModelBase
{
    public OverviewViewModel(IDataCollectionService dataCollectionService, Settings settings)
    {
        this.dataCollectionService = dataCollectionService;
        this.settings = settings;
    }

    private IDataCollectionService dataCollectionService;

    public IDataCollectionService DataCollectionService
    {
        get => dataCollectionService;
        set => Set(ref dataCollectionService, value);
    }

    private Settings settings;

    public Settings Settings
    {
        get => settings;
        set => Set(ref settings, value);
    }
}