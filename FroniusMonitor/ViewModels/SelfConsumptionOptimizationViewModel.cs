namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class SelfConsumptionOptimizationViewModel : ViewModelBase
{
    private readonly IWebClientService webClientService;
    private readonly IGen24JsonService gen24Service;
    private SelfConsumptionOptimizationView view=null!;
    private Gen24BatterySettings oldSettings = null!;

    public SelfConsumptionOptimizationViewModel(IWebClientService webClientService, IGen24JsonService gen24Service)
    {
        this.webClientService = webClientService;
        this.gen24Service = gen24Service;
    }

    private Gen24BatterySettings settings = null!;

    public Gen24BatterySettings Settings
    {
        get => settings;
        set => Set(ref settings, value);
    }

    private double logGridPower;

    public double LogGridPower
    {
        get => logGridPower;
        set => Set(ref logGridPower, value, UpdateGridPower);
    }

    private double logHomePower;

    public double LogHomePower
    {
        get => logHomePower;
        set => Set(ref logHomePower, value, UpdateHomePower);
    }

    private bool isFeedIn;

    public bool IsFeedIn
    {
        get => isFeedIn;
        set => Set(ref isFeedIn, value, UpdateGridPower);
    }

    private byte socMin;

    public byte SocMin
    {
        get => socMin;
        set => Set(ref socMin, value, () =>
        {
            Settings.SocMin = value;

            if (SocMax < value)
            {
                Settings.SocMax = SocMax = value;
            }
        });
    }

    private byte socMax;

    public byte SocMax
    {
        get => socMax;
        set => Set(ref socMax, value, () =>
        {
            Settings.SocMax = value;

            if (SocMin > value)
            {
                Settings.SocMin = SocMin = value;
            }
        });
    }

    private ICommand? undoCommand;
    public ICommand UndoCommand => undoCommand ??= new NoParameterCommand(Revert);

    private ICommand? applyCommand;
    public ICommand ApplyCommand => applyCommand ??= new NoParameterCommand(Apply);

    internal override async Task OnInitialize()
    {
        await base.OnInitialize().ConfigureAwait(false);
        view = IoC.Get<MainWindow>().SelfConsumptionOptimizationView;

        try
        {
            oldSettings = await webClientService.ReadGen24Entity<Gen24BatterySettings>("config/batteries").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show
                (
                    view, string.Format(Resources.InverterCommReadError, ex.Message),
                    ex.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error
                );

                view.Close();
            });

            return;
        }

        Revert();
    }

    private void UpdateGridPower()
    {
        Settings.RequestedGridPower = (int)Math.Round(Math.Pow(10, LogGridPower) * (IsFeedIn ? -1 : 1), MidpointRounding.AwayFromZero);
    }

    private void UpdateHomePower()
    {
        Settings.AcPowerMinimum = -(int)Math.Round(Math.Pow(10, LogHomePower), MidpointRounding.AwayFromZero);
    }

    private void Revert()
    {
        Settings = (Gen24BatterySettings)oldSettings.Clone();
        socMin = Settings.SocMin ?? 5;
        socMax = Settings.SocMax ?? 100;
        isFeedIn = Settings.RequestedGridPower < 0;
        NotifyOfPropertyChange(nameof(IsFeedIn));
        NotifyOfPropertyChange(nameof(SocMin));
        NotifyOfPropertyChange(nameof(SocMax));
        LogGridPower = Math.Log10(Math.Abs(Settings.RequestedGridPower ?? .0000001));
        LogHomePower = Math.Log10(Math.Abs(-Settings.AcPowerMinimum ?? .0000001));
    }

    private async void Apply()
    {
        var updateToken = gen24Service.GetUpdateToken(Settings, oldSettings);

        if (!updateToken.Children().Any())
        {
            await Dispatcher.InvokeAsync(() => MessageBox.Show(view, Resources.NoSettingsChanged, Resources.Warning, MessageBoxButton.OK, MessageBoxImage.Warning));
            return;
        }

        try
        {
            var _ = await webClientService.GetFroniusJsonResponse("config/batteries", updateToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(() => MessageBox.Show
            (
                view, string.Format(Resources.InverterCommError, ex.Message)+Environment.NewLine+Environment.NewLine+updateToken,
                ex.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error
            ));

            return;
        }

        oldSettings = Settings;
        Revert();
        //await Dispatcher.InvokeAsync(() => MessageBox.Show(view, Resources.SettingsWritten, Resources.Success, MessageBoxButton.OK, MessageBoxImage.Information));
    }
}