namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class MainViewModel(IDataCollectionService dataCollectionService, IFritzBoxService fritzBoxService, IWattPilotService wattPilotService) : ViewModelBase
{
    private bool wattPilotFirmwareUpdateMessageShown;

    private ICommand? exportSettingsCommand;
    public ICommand ExportSettingsCommand => exportSettingsCommand ??= new NoParameterCommand(ExportSettings);

    private ICommand? loadSettingsCommand;
    public ICommand LoadSettingsCommand => loadSettingsCommand ??= new NoParameterCommand(LoadSettings);

    private ICommand? downloadChargeLogCommand;
    public ICommand DownloadChargeLogCommand => downloadChargeLogCommand ??= new NoParameterCommand(DownloadChargeLog);

    private ICommand? rebootWattPilotCommand;
    public ICommand RebootWattPilotCommand => rebootWattPilotCommand ??= new NoParameterCommand(RebootWattPilot);

    public IDataCollectionService DataCollectionService => dataCollectionService;

    public IWattPilotService WattPilotService { get; } = wattPilotService;

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

        WattPilotService.NewFirmwareAvailable += (_, e) =>
        {
            if (!wattPilotFirmwareUpdateMessageShown)
            {
                wattPilotFirmwareUpdateMessageShown = true;

                Dispatcher.InvokeAsync(() => MessageBox.Show
                (
                    string.Format(Loc.NewFirmwareAvailable, e.NewFirmware, e.CurrentFirmware),
                    e.Name,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                ), DispatcherPriority.Background);
            }
        };

        await DataCollectionService.Start
        (
            App.Settings.FroniusConnection,
            App.Settings.FroniusConnection2,
            App.Settings.HaveFritzBox && App.Settings.ShowFritzBox ? App.Settings.FritzBoxConnection : null,
            App.Settings.HaveWattPilot && App.Settings.ShowWattPilot ? App.Settings.WattPilotConnection : null
        ).ConfigureAwait(false);

        if (!App.HaveSettings)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                IoC.Get<MainWindow>().GetView<SettingsView>().Focus();
            });
        }
    }

    internal void FritzBoxVisibilityChanged(bool isVisible)
    {
        fritzBoxService.Connection = isVisible ? App.Settings.FritzBoxConnection : null;

        if (isVisible)
        {
            DataCollectionService.InvalidateFritzBox();
        }

        _ = Settings.Save();
    }

    internal void WattPilotVisibilityChanged(bool isVisible)
    {
        DataCollectionService.WattPilotConnection = isVisible ? App.Settings.WattPilotConnection : null;
        _ = Settings.Save();
    }

    private async void RebootWattPilot()
    {
        WattPilotService.BeginSendValues();

        if (WattPilotService.WattPilot?.Clone() is not WattPilot newWattPilot)
        {
            await Dispatcher.InvokeAsync(() => MessageBox.Show(IoC.Get<MainWindow>(), Resources.NoWattPilot, Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error));
            return;
        }

        newWattPilot.Reboot = true;
        var errors = await WattPilotService.Send(newWattPilot, WattPilotService.WattPilot).ConfigureAwait(false);

        if (errors.Count > 0)
        {
            var notWritten = "• " + string.Join(Environment.NewLine + "• ", errors);

            await Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show
                (
                    IoC.Get<MainWindow>(),
                    "The following settings were not written to the Wattpilot:" + Environment.NewLine + Environment.NewLine + notWritten,
                    Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error
                );
            });

            return;
        }

        await WattPilotService.Stop().ConfigureAwait(false);
    }

    private void DownloadChargeLog()
    {
        var link = DataCollectionService.HomeAutomationSystem?.WattPilot?.DownloadLink;
        if (link == null) return;
        Process.Start(new ProcessStartInfo { FileName = link, UseShellExecute = true });
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

        DataCollectionService.Stop();

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
            await DataCollectionService.Start
            (
                App.Settings.FroniusConnection,
                App.Settings.HaveTwoInverters ? App.Settings.FroniusConnection2 : null,
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
