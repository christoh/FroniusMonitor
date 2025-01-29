namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class BatteryDetailsViewModel : ViewModelBase
{
    public string Title => Loc.BatteryDetailsView + " " + Header;

    public string Header
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(Title)));
    } = string.Empty;

    public Gen24Storage? Battery
    {
        get;
        set => Set(ref field, value);
    }

    public HomeAutomationSystem? HomeAutomationSystem
    {
        get;
        set => Set(ref field, value);
    }

    internal void OnNewDataReceived(HomeAutomationSystem? h)
    {
        HomeAutomationSystem = h;
        Battery = h?.Gen24Sensors?.Storage;
        Header = $"{Battery?.Model} (HW: {Battery?.HardwareVersion?.ToLinuxString() ?? "---"} / FW: {Battery?.SoftwareVersion?.ToLinuxString() ?? "---"}) - {Battery?.StatusString ?? Loc.Unknown}";
    }
}