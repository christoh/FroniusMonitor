namespace FroniusPhone.ViewModels;

public class OverviewViewModel : ViewModelBase
{
    public OverviewViewModel(ISolarSystemService solarSystemService, Settings settings)
    {
        this.solarSystemService = solarSystemService;
        this.settings = settings;
    }

    private ISolarSystemService solarSystemService;

    public ISolarSystemService SolarSystemService
    {
        get => solarSystemService;
        set => Set(ref solarSystemService, value);
    }

    private Settings settings;

    public Settings Settings
    {
        get => settings;
        set => Set(ref settings, value);
    }
}