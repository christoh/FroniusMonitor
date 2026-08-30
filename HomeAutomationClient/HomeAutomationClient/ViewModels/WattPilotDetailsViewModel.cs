using De.Hochstaetter.Fronius.Models.Charging;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public partial class WattPilotDetailsViewModel(MainViewModel mainViewModel) : ViewModelBase
{
    /// <summary>
    /// The WattPilot to show. Assign it before the view is displayed, like the inverter and battery views.
    /// </summary>
    [ObservableProperty]
    public partial WattPilot WattPilot { get; set; } = null!;

    /// <summary>
    /// The gauge settings live on the singleton main view model, next to the switch in the main view.
    /// </summary>
    public MainViewModel MainViewModel => mainViewModel;
}
