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

    private bool enableDanger;

    public bool EnableDanger
    {
        get => enableDanger;
        set => Set(ref enableDanger, value, () =>
        {
            if (!value)
            {
                Settings.IsEnabled = oldSettings.IsEnabled;
                Settings.IsInCalibration=oldSettings.IsInCalibration;
            }
        });
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

    private int? acPowerMinimum;

    public int? AcPowerMinimum
    {
        get => acPowerMinimum;
        set => Set(ref acPowerMinimum, value,UpdateLogHomePower);
    }

    private int? requestedGridPower;

    public int? RequestedGridPower
    {
        get => requestedGridPower;
        set => Set(ref requestedGridPower, value,UpdateLogGridPower);
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
        RequestedGridPower= (int)Math.Round(Math.Pow(10, LogGridPower), MidpointRounding.AwayFromZero);
        Settings.RequestedGridPower = RequestedGridPower * (IsFeedIn ? -1 : 1);
    }

    private void UpdateHomePower()
    {
        AcPowerMinimum=Settings.AcPowerMinimum = -(int)Math.Round(Math.Pow(10, LogHomePower), MidpointRounding.AwayFromZero);
    }

    private void UpdateLogHomePower()
    {
        Settings.AcPowerMinimum=AcPowerMinimum;
        LogHomePower = Math.Log10(-AcPowerMinimum??double.NegativeInfinity);
    }

    private void UpdateLogGridPower()
    {
        Settings.RequestedGridPower = RequestedGridPower*(IsFeedIn ? -1 : 1);
        LogGridPower = Math.Log10(RequestedGridPower ?? double.NegativeInfinity);
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
        var errors = view.FindVisualChildren<TextBox>().SelectMany(Validation.GetErrors).ToArray();

        foreach (var error in errors)
        {
            if (error.BindingInError is BindingExpression {Target: FrameworkElement {IsVisible: false}} expression)
            {
                var property = oldSettings.GetType().GetProperty(expression.ResolvedSourcePropertyName);

                if (property != null)
                {
                    var value = property.GetValue(oldSettings);
                    property.SetValue(Settings, value);
                }
            }
        }

        var errorList = errors
            .Where(e => e.BindingInError is BindingExpression {Target: FrameworkElement {IsVisible: true}})
            .Select(e => e.ErrorContent.ToString()).ToArray();

        if (errorList.Length > 0)
        {
            MessageBox.Show
            (
                view,
                $"{Resources.PleaseCorrectErrors}:{Environment.NewLine}{errorList.Aggregate(string.Empty, (c, n) => c + Environment.NewLine + "• " + n)}",
                Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error
            );

            return;
        }

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