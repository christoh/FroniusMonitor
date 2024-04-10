namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class BatteryDetailsViewModel : ViewModelBase
{
    public string Title => Loc.BatteryDetailsView + " " + Header;

    private string header = string.Empty;

    public string Header
    {
        get => header;
        set => Set(ref header, value, () => NotifyOfPropertyChange(nameof(Title)));
    }

    private Gen24Storage? battery;

    public Gen24Storage? Battery
    {
        get => battery;
        set => Set(ref battery, value);
    }

    private HomeAutomationSystem? homeAutomationSystem;

    public HomeAutomationSystem? HomeAutomationSystem
    {
        get => homeAutomationSystem;
        set => Set(ref homeAutomationSystem, value);
    }

    internal void OnNewDataReceived(HomeAutomationSystem? h)
    {
        HomeAutomationSystem = h;
        Battery = h?.Gen24Sensors?.Storage;
        Header = $"{Battery?.Model} (HW: {Battery?.HardwareVersion?.ToLinuxString() ?? "---"} / FW: {Battery?.SoftwareVersion?.ToLinuxString() ?? "---"}) - {Battery?.StatusString ?? Loc.Unknown}";
    }
}