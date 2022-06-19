namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IWebClientService webClientService;

    public MainViewModel(ISolarSystemService solarSystemService, IWebClientService webClientService, IWattPilotService wattPilotService)
    {
        this.webClientService = webClientService;
        SolarSystemService = solarSystemService;
        WattPilotService = wattPilotService;
        ExportSettingsCommand = new NoParameterCommand(ExportSettings);
        LoadSettingsCommand = new NoParameterCommand(LoadSettings);
        DownloadChargeLogCommand = new NoParameterCommand(DownloadChargeLog);
    }

    public ICommand ExportSettingsCommand { get; }
    public ICommand LoadSettingsCommand { get; }
    public ICommand DownloadChargeLogCommand { get; }

    public ISolarSystemService SolarSystemService { get; }

    public IWattPilotService WattPilotService { get; }

    public bool IncludeInverterPower
    {
        get => App.Settings.AddInverterPowerToConsumption;
        set
        {
            if (value != App.Settings.AddInverterPowerToConsumption)
            {
                App.Settings.AddInverterPowerToConsumption = value;
                NotifyOfPropertyChange();
                Settings.Save().ConfigureAwait(false);
            }
        }
    }

    public Settings Settings => App.Settings;

    internal override async Task OnInitialize()
    {
        await base.OnInitialize().ConfigureAwait(false);

        await SolarSystemService.Start
        (
            App.Settings.FroniusConnection,
            App.Settings.HaveFritzBox && App.Settings.ShowFritzBox ? App.Settings.FritzBoxConnection : null,
            App.Settings.HaveWattPilot && App.Settings.ShowWattPilot ? App.Settings.WattPilotConnection : null
        ).ConfigureAwait(false);
    }

    internal void FritzBoxVisibilityChanged(bool isVisible)
    {
        webClientService.FritzBoxConnection = isVisible ? App.Settings.FritzBoxConnection : null;

        if (isVisible)
        {
            SolarSystemService.InvalidateFritzBox();
        }

        _ = Settings.Save();
    }

    internal void WattPilotVisibilityChanged(bool isVisible)
    {
        SolarSystemService.WattPilotConnection = isVisible ? App.Settings.WattPilotConnection : null;
        _ = Settings.Save();
    }

    private void DownloadChargeLog()
    {
        var link = SolarSystemService.SolarSystem?.WattPilot?.DownloadLink;
        if (link == null) return;
        Process.Start(new ProcessStartInfo {FileName = link, UseShellExecute = true});
    }

    private async void LoadSettings()
    {
        var dialog = new OpenFileDialog
        {
            Filter = string.Format(Resources.SettingsFilter, Resources.AppName),
            AddExtension = false,
            CheckFileExists = true,
            CheckPathExists = true,
            DereferenceLinks = true,
            Multiselect = false,
            ValidateNames = true,
            Title = Resources.LoadSettings
        };

        var result = dialog.ShowDialog();

        if (!result.HasValue || !result.Value)
        {
            return;
        }

        SolarSystemService.Stop();

        try
        {
            try
            {
                await Settings.Load(dialog.FileName);
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() => MessageBox.Show(ex.Message, Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error));
                return;
            }

            await Settings.Save().ConfigureAwait(false);
        }
        finally
        {
            await SolarSystemService.Start
            (
                App.Settings.FroniusConnection,
                App.Settings.HaveFritzBox && App.Settings.ShowFritzBox ? App.Settings.FritzBoxConnection : null,
                App.Settings.HaveWattPilot && App.Settings.ShowWattPilot ? App.Settings.WattPilotConnection : null
            ).ConfigureAwait(false);
        }
    }

    private async void ExportSettings()
    {
        var dialog = new SaveFileDialog
        {
            AddExtension = false,
            CheckPathExists = true,
            OverwritePrompt = true,
            Filter = string.Format(Resources.SettingsFilter, Resources.AppName),
            DereferenceLinks = true,
            FileName = Path.GetFileName(App.SettingsFileName),
            Title = Resources.ExportSettings,
            ValidateNames = true,
        };

        var result = dialog.ShowDialog();

        if (!result.HasValue || !result.Value)
        {
            return;
        }

        try
        {
            await Settings.Save(dialog.FileName).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(() => MessageBox.Show(ex.Message, Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error));
        }
    }
}
