namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class SmartMeterDetailsViewModel : ViewModelBase
{
    private string title = string.Empty;

    public string Title
    {
        get => title;
        set => Set(ref title, value);
    }

    private string header = string.Empty;

    public string Header
    {
        get => header;
        set => Set(ref header, value);
    }

    private HomeAutomationSystem homeAutomationSystem = new();

    public HomeAutomationSystem HomeAutomationSystem
    {
        get => homeAutomationSystem;
        set => Set(ref homeAutomationSystem, value);
    }

    private Gen24PowerMeter3P? meter;

    public Gen24PowerMeter3P? Meter
    {
        get => meter;
        set => Set(ref meter, value);
    }

    private Gen24Config? config;

    public Gen24Config? Config
    {
        get => config;
        set => Set(ref config, value);
    }

    private Gen24Status? status;

    public Gen24Status? Status
    {
        get => status;
        set => Set(ref status, value);
    }

    internal void OnNewDataReceived(HomeAutomationSystem newHomeAutomationSystem)
    {
        HomeAutomationSystem = newHomeAutomationSystem;
        Status = newHomeAutomationSystem.Gen24Sensors?.MeterStatus;
        Meter = newHomeAutomationSystem.Gen24Sensors?.PrimaryPowerMeter;
        Config = newHomeAutomationSystem.Gen24Config;
        Header = $"{Meter?.Model ?? Loc.Unknown} ({Meter?.SoftwareVersion?.ToLinuxString() ?? "---"}) - {Status?.StatusMessage}";
        Title = $"{Loc.SmartMeterDetailsView} - {Header}";
    }
}
