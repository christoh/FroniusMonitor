namespace FroniusPhone.ViewModels;

public class MainViewModel : BindableBase
{
    public MainViewModel(ISolarSystemService solarSystemService)
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