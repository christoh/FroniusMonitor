using De.Hochstaetter.Fronius.Extensions;

namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class WattPilotSettingsViewModel : ViewModelBase
{
    private readonly ISolarSystemService solarSystemService;
    private readonly IWattPilotService wattPilotService;
    private WattPilot oldWattPilot = null!;
    private readonly MainWindow mainWindow;

    public WattPilotSettingsViewModel(ISolarSystemService solarSystemService, IWattPilotService wattPilotService, MainWindow mainWindow)
    {
        this.solarSystemService = solarSystemService;
        this.wattPilotService = wattPilotService;
        this.mainWindow = mainWindow;
    }

    public static IReadOnlyList<CableLockBehavior> CableLockBehaviors { get; } = Enum.GetValues<CableLockBehavior>();
    public static IReadOnlyList<ChargingLogic> ChargingLogicList { get; } = Enum.GetValues<ChargingLogic>();
    public static IReadOnlyList<EcoRoundingMode> EcoRoundingModes { get; } = Enum.GetValues<EcoRoundingMode>();
    public static IReadOnlyList<PhaseSwitchMode> PhaseSwitchModes { get; } = Enum.GetValues<PhaseSwitchMode>();
    public static IReadOnlyList<AwattarCountry> EnergyPriceCountries { get; } = Enum.GetValues<AwattarCountry>().OrderBy(c => c.ToDisplayName()).ToArray();
    public static IReadOnlyList<ForcedCharge> ForcedChargeList { get; } = Enum.GetValues<ForcedCharge>().OrderBy(c => c.ToDisplayName()).ToArray();


    private WattPilot wattPilot = null!;

    public WattPilot WattPilot
    {
        get => wattPilot;
        set => Set(ref wattPilot, value, () =>
        {
            NotifyOfPropertyChange(nameof(ApiLink));
            NotifyOfPropertyChange(nameof(ApiUri));
        });
    }

    private bool enableDanger;

    public bool EnableDanger
    {
        get => enableDanger;
        set => Set(ref enableDanger, value);
    }

    private string toastText=string.Empty;

    public string ToastText
    {
        get => toastText;
        set => Set(ref toastText, value);
    }

    private string title = "Wattpilot";

    public string Title
    {
        get => title;
        set => Set(ref title, value);
    }

    private bool isInUpdate;

    public bool IsInUpdate
    {
        get => isInUpdate;
        set => Set(ref isInUpdate, value);
    }

    private bool requiresChargingInterval;

    public bool RequiresChargingInterval
    {
        get => requiresChargingInterval;
        set => Set(ref requiresChargingInterval, value, () =>
        {
            if (value && WattPilot.MinimumChargingInterval < 300000)
            {
                WattPilot.MinimumChargingInterval = 300000;
            }
        });
    }

    private bool isUserAuthenticated;

    public bool IsUserAuthenticated
    {
        get => isUserAuthenticated;
        set => Set(ref isUserAuthenticated, value);
    }

    public string ApiLink => "https://" + (WattPilot.SerialNumber ?? "<Serial>") + ".api.v3.go-e.io";
    public string ApiUri => ApiLink + $"/api/status?token={WattPilot.CloudAccessKey ?? "<Token>"}";

    private ICommand? applyCommand;
    public ICommand ApplyCommand => applyCommand ??= new NoParameterCommand(Apply);

    private ICommand? undoCommand;
    public ICommand UndoCommand => undoCommand ??= new NoParameterCommand(Undo);

    private ICommand? navigateToApiCommand;
    public ICommand NavigateToApiCommand => navigateToApiCommand ??= new NoParameterCommand(NavigateToApi);

    private void NavigateToApi()
    {
        Process.Start(new ProcessStartInfo(ApiUri) {UseShellExecute = true});
    }


    internal override async Task OnInitialize()
    {
        await base.OnInitialize().ConfigureAwait(false);
        var localWattPilot = wattPilotService.WattPilot ?? solarSystemService.SolarSystem?.WattPilot;

        if (localWattPilot == null)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show(Resources.NoWattPilot, Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                mainWindow.SettingsView.Close();
                mainWindow.DataContext = null;
            });

            return;
        }

        IsUserAuthenticated = localWattPilot.AuthenticatedCardIndex.HasValue;
        oldWattPilot = (WattPilot)localWattPilot.Clone();

        ////Reboot WattPilot
        //oldWattPilot.Reboot = true;
        //await wattPilotService.SendValue(oldWattPilot, nameof(Fronius.Models.Charging.WattPilot.Reboot));
        Undo();
    }

    public void Undo()
    {
        WattPilot = (WattPilot)oldWattPilot.Clone();
        Title = $"{WattPilot.DeviceName} {WattPilot.SerialNumber}";
        RequiresChargingInterval = (WattPilot.MinimumChargingInterval ?? 0) != 0;

        if (WattPilot.AllowChargingPause.HasValue && !WattPilot.AllowChargingPause.Value)
        {
            WattPilot.PhaseSwitchMode = PhaseSwitchMode.Phases3;
        }
    }

    public async void Apply()
    {
        try
        {
            IsInUpdate = true;

            if (IsUserAuthenticated)
            {
                WattPilot.AuthenticatedCardIndex ??= 0;
            }
            else
            {
                WattPilot.AuthenticatedCardIndex = null;
            }

            if (!RequiresChargingInterval)
            {
                WattPilot.MinimumChargingInterval = 0;
            }

            if (WattPilot.AllowChargingPause.HasValue && !WattPilot.AllowChargingPause.Value)
            {
                WattPilot.PhaseSwitchMode = PhaseSwitchMode.Phases3;
            }

            wattPilotService.BeginSendValues();
            var sentSomething = false;
            try
            {
                var errors = await wattPilotService.Send(WattPilot, oldWattPilot).ConfigureAwait(false);
                sentSomething = true;

                if (errors.Count > 0)
                {
                    var notWritten = "• " + string.Join(Environment.NewLine + "• ", errors);

                    await Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show
                        (
                            mainWindow.WattPilotSettingsView,
                            "The following settings were not written to the Wattpilot:" + Environment.NewLine + Environment.NewLine + notWritten,
                            Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error
                        );
                    });
                }
            }
            catch (ArgumentException ex)
            {
                sentSomething=false;
                IsInUpdate = false;
                Dispatcher.Invoke(() => MessageBox.Show(ex.Message, Resources.Warning, MessageBoxButton.OK, MessageBoxImage.Warning));
            }
            finally
            {
                try
                {
                    await wattPilotService.WaitSendValues().ConfigureAwait(false);

                    if (sentSomething)
                    {
                        ToastText = Resources.SentToWattPilot;
                    }

                    oldWattPilot = WattPilot;
                    Undo();
                }
                catch (TimeoutException) when (wattPilotService.UnsuccessfulWrites.Count > 0)
                {
                    var notWritten = "• " + string.Join(Environment.NewLine + "• ", wattPilotService.UnsuccessfulWrites.Select(a => a.ToString()));
                    IsInUpdate = false;

                    await Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show
                        (
                            mainWindow.WattPilotSettingsView,
                            "The following settings were not confirmed by the Wattpilot:" + Environment.NewLine + Environment.NewLine + notWritten,
                            Resources.Warning, MessageBoxButton.OK, MessageBoxImage.Warning
                        );
                    });
                }
            }
        }
        finally
        {
            IsInUpdate = false;
        }
    }
}
