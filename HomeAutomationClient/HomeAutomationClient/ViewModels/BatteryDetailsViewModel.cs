namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public partial class BatteryDetailsViewModel(MainViewModel mainViewModel) : ViewModelBase
{
    /// <summary>
    /// The inverter the battery belongs to. It carries both the battery itself (<c>Sensors.Storage</c>) and the
    /// net values (<c>NetStateOfChange</c>, <c>StorageNetCapacity</c>, <c>MaxStorageNetCapacity</c>), which the WPF
    /// version took from its HomeAutomationSystem.
    /// </summary>
    [ObservableProperty]
    public partial Gen24System Gen24System { get; set; } = null!;

    /// <summary>
    /// The gauge settings live on the singleton main view model, next to the switch in the main view.
    /// </summary>
    public MainViewModel MainViewModel => mainViewModel;
}
