using CommunityToolkit.Mvvm.ComponentModel;

namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public partial class BatteryDetailsViewModel : ViewModelBase
{
    public string Title => Loc.BatteryDetailsView + " " + Header;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial string Header { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Gen24Storage? Battery { get; set; }

    [ObservableProperty]
    public partial HomeAutomationSystem? HomeAutomationSystem { get; set; }

    internal void OnNewDataReceived(HomeAutomationSystem? h)
    {
        HomeAutomationSystem = h;
        Battery = h?.Gen24Sensors?.Storage;
        Header = $"{Battery?.Model} (HW: {Battery?.HardwareVersion?.ToLinuxString() ?? "---"} / FW: {Battery?.SoftwareVersion?.ToLinuxString() ?? "---"}) - {Battery?.StatusString ?? Loc.Unknown}";
    }
}