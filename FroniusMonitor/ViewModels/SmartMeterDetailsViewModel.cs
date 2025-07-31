namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class SmartMeterDetailsViewModel : ViewModelBase
{
    public string Title
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    public string Header
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    public HomeAutomationSystem HomeAutomationSystem
    {
        get;
        set => Set(ref field, value);
    } = new();

    public Gen24PowerMeter3P? Meter
    {
        get;
        set => Set(ref field, value);
    }

    public Gen24Config? Config
    {
        get;
        set => Set(ref field, value);
    }

    public Gen24Status? Status
    {
        get;
        set => Set(ref field, value);
    }

    internal void OnNewDataReceived(HomeAutomationSystem newHomeAutomationSystem)
    {
        HomeAutomationSystem = newHomeAutomationSystem;
        Status = newHomeAutomationSystem.Gen24Sensors?.MeterStatus;
        Meter = newHomeAutomationSystem.Gen24Sensors?.PrimaryPowerMeter;
        Config = newHomeAutomationSystem.Gen24Config;
        Header = $"{Meter?.Model ?? Loc.Unknown} ({Meter?.SoftwareVersion?.ToLinuxString() ?? "---"}) - {Status?.StatusMessage()}";
        Title = $"{Loc.SmartMeterDetailsView} - {Header}";
    }
}
