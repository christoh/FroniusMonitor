namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class MainViewModel(IDataCollectionService dataCollectionService, IFritzBoxService fritzBoxService, IWattPilotService wattPilotService) : ViewModelBase
{
    private bool wattPilotFirmwareUpdateMessageShown;
    private bool isGarbageCollecting;

    private ICommand? exportSettingsCommand;
    public ICommand ExportSettingsCommand => exportSettingsCommand ??= new NoParameterCommand(ExportSettings);

    private ICommand? loadSettingsCommand;
    public ICommand LoadSettingsCommand => loadSettingsCommand ??= new NoParameterCommand(LoadSettings);

    private ICommand? downloadChargeLogCommand;
    public ICommand DownloadChargeLogCommand => downloadChargeLogCommand ??= new NoParameterCommand(DownloadChargeLog);

    private ICommand? garbageCollectionCommand;
    public ICommand GarbageCollectionCommand => garbageCollectionCommand ??= new NoParameterCommand(GarbageCollection);

    private ICommand? importBayernwerkCommand;
    public ICommand ImportBayernwerkCommand => importBayernwerkCommand ??= new NoParameterCommand(ImportBayernwerkFile);

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

    private void DownloadChargeLog()
    {
        try
        {
            WattPilotService.OpenChargingLog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(IoC.GetRegistered<MainWindow>(), ex.Message, Loc.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LoadSettings()
    {
        var dialog = new OpenFileDialog
        {
            Filter = string.Format(Loc.SettingsFilter, Loc.AppName),
            AddExtension = false,
            CheckFileExists = true,
            CheckPathExists = true,
            DereferenceLinks = true,
            Multiselect = false,
            ValidateNames = true,
            Title = Loc.LoadSettings
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
                await Dispatcher.InvokeAsync(() => MessageBox.Show(ex.Message, Loc.Error, MessageBoxButton.OK, MessageBoxImage.Error));
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

    private async void GarbageCollection()
    {
        if (isGarbageCollecting)
        {
            return;
        }

        try
        {
            isGarbageCollecting = true;

            await Task.Run(() =>
            {
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
            }).ConfigureAwait(false);
        }
        finally
        {
            isGarbageCollecting = false;
        }
    }

    private async void ExportSettings()
    {
        var dialog = new SaveFileDialog
        {
            AddExtension = false,
            CheckPathExists = true,
            OverwritePrompt = true,
            Filter = string.Format(Loc.SettingsFilter, Loc.AppName),
            DereferenceLinks = true,
            FileName = Path.GetFileName(App.SettingsFileName),
            Title = Loc.ExportSettings,
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
            await Dispatcher.InvokeAsync(() => MessageBox.Show(ex.Message, Loc.Error, MessageBoxButton.OK, MessageBoxImage.Error));
        }
    }

    private async void ImportBayernwerkFile()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = Loc.FilterExcelFile + Loc.FilterAllFiles,
            CheckFileExists = true,
            CheckPathExists = true,
            Multiselect = false,
            ShowHiddenItems = false,
            ValidateNames = true,
            Title = Loc.ImportBayernwerk,
        };

        var result = openFileDialog.ShowDialog();

        if (result is not true)
        {
            return;
        }

        try
        {
            await dataCollectionService.ImportBayernwerkExcelFile(openFileDialog.FileName).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Loc.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
