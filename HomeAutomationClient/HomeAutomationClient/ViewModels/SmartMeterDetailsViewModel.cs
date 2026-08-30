namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public partial class SmartMeterDetailsViewModel(IUpdateService updateService, MainViewModel mainViewModel) : ViewModelBase
{
    /// <summary>
    /// The meter, its status and the inverter config with the export limits all come from the update service, so
    /// unlike the inverter and battery views this one needs no device to be assigned before it is shown.
    /// </summary>
    /// <remarks>
    /// Binding the live SmartMeter property is also what keeps the gauges moving: every update replaces the meter
    /// object (Gen24System.CopyFrom assigns a new Sensors), so a meter handed over once would freeze.
    /// </remarks>
    public IUpdateService UpdateService => updateService;

    /// <summary>
    /// The gauge settings live on the singleton main view model, next to the switch in the main view.
    /// </summary>
    public MainViewModel MainViewModel => mainViewModel;
}
