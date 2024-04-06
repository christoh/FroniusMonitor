using De.Hochstaetter.Fronius.Models.Gen24;

namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class InverterDetailsViewModel(
    IDataCollectionService dataCollectionService,
    IGen24Service gen24Service
) : ViewModelBase
{
    private string title = string.Empty;

    public string Title
    {
        get => title;
        set => Set(ref title, value);
    }

    private Gen24System inverter = null!;

    public Gen24System Inverter
    {
        get => inverter;
        set => Set(ref inverter, value);
    }

    private bool isSecondary;

    public bool IsSecondary
    {
        get => isSecondary;
        set => Set(ref isSecondary, value);
    }

    internal override async Task OnInitialize()
    {
        try
        {
            IsSecondary = ReferenceEquals(gen24Service, dataCollectionService.Gen24Service2);
            dataCollectionService.NewDataReceived += OnNewDataReceived;

            Inverter = new Gen24System
            {
                Service = gen24Service,
            };

            OnNewDataReceived();
        }

        finally
        {
            await base.OnInitialize().ConfigureAwait(false);
        }
    }

    private void OnNewDataReceived(object? s = null, SolarDataEventArgs? e = null)
    {
        Inverter.Config = IsSecondary ? dataCollectionService.HomeAutomationSystem?.Gen24Config2 : dataCollectionService.HomeAutomationSystem?.Gen24Config;
        Inverter.Sensors = IsSecondary ? dataCollectionService.HomeAutomationSystem?.Gen24Sensors2 : dataCollectionService.HomeAutomationSystem?.Gen24Sensors;
        Title = $"{Inverter.Config?.InverterSettings?.SystemName ?? "---"} - {Inverter.Config?.Versions?.ModelName ?? "---"} ({Inverter.Sensors?.InverterStatus?.StatusMessage ?? Loc.Unknown})";
    }
}