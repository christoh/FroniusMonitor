namespace FroniusPhone.ViewModels;

public class OverviewViewModel : BindableBase
{
    public OverviewViewModel(ISolarSystemService solarSystemService)
    {
        this.solarSystemService = solarSystemService;
    }

    private ISolarSystemService solarSystemService;

    public ISolarSystemService SolarSystemService
    {
        get => solarSystemService;
        set => Set(ref solarSystemService, value);
    }
}