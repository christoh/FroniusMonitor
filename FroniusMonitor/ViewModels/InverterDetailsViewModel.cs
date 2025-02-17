namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class InverterDetailsViewModel(
    IDataCollectionService dataCollectionService,
    IGen24Service gen24Service
) : ViewModelBase
{

    public string Title => Loc.InverterDetailsView + " " + Header;

    public string Header
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(Title)));
    } = string.Empty;

    public Gen24System Inverter
    {
        get;
        set => Set(ref field, value);
    } = null!;

    public bool IsSecondary
    {
        get;
        set => Set(ref field, value);
    }

    public bool IsNoneSelected
    {
        get;
        set => Set(ref field, value);
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

    internal override Task CleanUp()
    {
        dataCollectionService.NewDataReceived -= OnNewDataReceived;
        return base.CleanUp();
    }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private void OnNewDataReceived(object? s = null, SolarDataEventArgs? e = null)
    {
        Inverter.Config = IsSecondary ? dataCollectionService.HomeAutomationSystem?.Gen24Config2 : dataCollectionService.HomeAutomationSystem?.Gen24Config;
        Inverter.Sensors = IsSecondary ? dataCollectionService.HomeAutomationSystem?.Gen24Sensors2 : dataCollectionService.HomeAutomationSystem?.Gen24Sensors;
        Header = $"{Inverter.Config?.InverterSettings?.SystemName ?? "---"} - {Inverter.Config?.Versions?.ModelName ?? "---"} ({Inverter.Config?.Versions?.SwVersions["GEN24"].ToLinuxString() ?? "0.0.0-0"}) - {Inverter.Sensors?.InverterStatus?.StatusMessageCaption ?? Loc.Unknown}";
    }
}