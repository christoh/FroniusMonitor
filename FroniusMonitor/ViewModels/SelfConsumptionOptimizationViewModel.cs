namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class SelfConsumptionOptimizationViewModel : SettingsViewModelBase
{
    private static readonly IEnumerable<ChargingRuleType> ruleTypes = Enum.GetValues<ChargingRuleType>();

    private SelfConsumptionOptimizationView view = null!;
    private Gen24BatterySettings oldSettings = null!;
    private BindableCollection<Gen24ChargingRule> oldChargingRules = null!;

    public SelfConsumptionOptimizationViewModel(IWebClientService webClientService, IGen24JsonService gen24Service) : base(webClientService, gen24Service)
    {
    }

    public IEnumerable<ChargingRuleType> RuleTypes => ruleTypes;

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
                Settings.IsInCalibration = oldSettings.IsInCalibration;
                Settings.IsAcCoupled = oldSettings.IsAcCoupled;
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
        set => Set(ref acPowerMinimum, value, UpdateLogHomePower);
    }

    private int? requestedGridPower;

    public int? RequestedGridPower
    {
        get => requestedGridPower;
        set => Set(ref requestedGridPower, value, UpdateLogGridPower);
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

    private BindableCollection<Gen24ChargingRule> chargingRules = null!;

    public BindableCollection<Gen24ChargingRule> ChargingRules
    {
        get => chargingRules;
        set => Set(ref chargingRules, value);
    }

    private ICommand? undoCommand;
    public ICommand UndoCommand => undoCommand ??= new NoParameterCommand(Undo);

    private ICommand? applyCommand;
    public ICommand ApplyCommand => applyCommand ??= new NoParameterCommand(Apply);

    private ICommand? deleteChargingRuleCommand;
    public ICommand DeleteChargingRuleCommand => deleteChargingRuleCommand ??= new Command<Gen24ChargingRule>(DeleteChargingRule!);

    private ICommand? addChargingRuleCommand;
    public ICommand AddChargingRuleCommand => addChargingRuleCommand ??= new NoParameterCommand(AddChargingRule);

    internal override async Task OnInitialize()
    {
        await base.OnInitialize().ConfigureAwait(false);
        var mainWindow = IoC.Get<MainWindow>();
        view = mainWindow.SelfConsumptionOptimizationView;
        string jsonString;

        try
        {
            jsonString = (await WebClientService.GetFroniusStringResponse("config/timeofuse").ConfigureAwait(false)).JsonString;
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
                mainWindow.Activate();
            });

            return;
        }

        await Dispatcher.InvokeAsync(() => oldChargingRules = Gen24ChargingRule.Parse(jsonString));

        try
        {
            oldSettings = await WebClientService.ReadGen24Entity<Gen24BatterySettings>("config/batteries").ConfigureAwait(false);
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

        Undo();
    }

    private void UpdateGridPower()
    {
        RequestedGridPower = (int)Math.Round(Math.Pow(10, LogGridPower), MidpointRounding.AwayFromZero);
        Settings.RequestedGridPower = RequestedGridPower * (IsFeedIn ? -1 : 1);
    }

    private void UpdateHomePower()
    {
        AcPowerMinimum = Settings.AcPowerMinimum = -(int)Math.Round(Math.Pow(10, LogHomePower), MidpointRounding.AwayFromZero);
    }

    private void UpdateLogHomePower()
    {
        Settings.AcPowerMinimum = AcPowerMinimum;
        LogHomePower = Math.Log10(-AcPowerMinimum ?? double.NegativeInfinity);
    }

    private void UpdateLogGridPower()
    {
        Settings.RequestedGridPower = RequestedGridPower * (IsFeedIn ? -1 : 1);
        LogGridPower = Math.Log10(RequestedGridPower ?? double.NegativeInfinity);
    }

    private void DeleteChargingRule(Gen24ChargingRule rule)
    {
        ChargingRules.Remove(rule);
    }

    private void AddChargingRule()
    {
        ChargingRules.Add(new Gen24ChargingRule
        {
            IsActive = false,
            RuleType = ChargingRuleType.MaximumCharge,
            Power = 50000,
            StartTime = "00:00",
            EndTime = "00:00",
            Monday = true,
            Tuesday = true,
            Wednesday = true,
            Thursday = true,
            Friday = true,
            Saturday = true,
            Sunday = true,
        });
    }

    private void Undo()
    {
        ChargingRules = (BindableCollection<Gen24ChargingRule>)oldChargingRules.Clone();
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
        IsInUpdate = true;

        try
        {
            var errors = view.FindVisualChildren<TextBox>().SelectMany(Validation.GetErrors).ToArray();

            foreach (var error in errors)
            {
                if
                (
                    error.BindingInError is BindingExpression { Target: FrameworkElement { IsVisible: false } } expression &&
                    oldSettings.GetType().GetProperty(expression.ResolvedSourcePropertyName) is { } property
                )
                {
                    var value = property.GetValue(oldSettings);
                    property.SetValue(Settings, value);
                }
            }

            var errorList = errors
                .Where(e => e.BindingInError is BindingExpression { Target: FrameworkElement { IsVisible: true } })
                .Select(e => e.ErrorContent.ToString()).ToList();

            for (var i = 0; i < ChargingRules.Count; i++)
            {
                if (ChargingRules[i].StartTimeDate > ChargingRules[i].EndTimeDate)
                {
                    errorList.Add(string.Format(Resources.EndBeforeStart, ChargingRules[i]));
                }

                if (i < ChargingRules.Count - 1)
                {
                    for (var j = i + 1; j < ChargingRules.Count; j++)
                    {
                        if (ChargingRules[i].ConflictsWith(ChargingRules[j]))
                        {
                            errorList.Add(string.Format(Resources.ChargingRuleConflict, ChargingRules[i], ChargingRules[j]));
                        }
                    }
                }
            }

            if (errorList.Count > 0)
            {
                MessageBox.Show
                (
                    view,
                    $"{Resources.PleaseCorrectErrors}:{Environment.NewLine}{errorList.Aggregate(string.Empty, (c, n) => c + Environment.NewLine + "• " + n)}",
                    Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error
                );

                return;
            }

            var rulesChanged = !ChargingRules.SequenceEqual(oldChargingRules);
            var updateToken = Gen24Service.GetUpdateToken(Settings, oldSettings);
            var nonRulesChanged = updateToken.Children().Any();
            var rulesToken = Gen24ChargingRule.GetToken(ChargingRules);

            if (!updateToken.Children().Any() && !rulesChanged)
            {
                ShowNoSettingsChanged();
                return;
            }

            if (nonRulesChanged)
            {
                if (!await UpdateInverter("config/batteries", updateToken).ConfigureAwait(false))
                {
                    return;
                }
            }

            if (rulesChanged)
            {
                if (!await UpdateInverter("config/timeofuse", rulesToken).ConfigureAwait(false))
                {
                    return;
                }
            }

            oldSettings = Settings;
            oldChargingRules = ChargingRules;
            Undo();
            ToastText = Resources.SettingsSavedToInverter;
        }
        finally
        {
            IsInUpdate = false;
        }
    }
}
