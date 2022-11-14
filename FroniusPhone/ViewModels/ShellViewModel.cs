namespace FroniusPhone.ViewModels;

public class ShellViewModel : BindableBase
{
    public ShellViewModel(ISolarSystemService solarSystemService)
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