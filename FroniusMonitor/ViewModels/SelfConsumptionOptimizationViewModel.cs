﻿using CommunityToolkit.Mvvm.ComponentModel;

namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class SelfConsumptionOptimizationViewModel
(
    IGen24Service gen24Service,
    IGen24JsonService gen24JsonService,
    IFritzBoxService fritzBoxService,
    IWattPilotService wattPilotService,
    IDataCollectionService dataCollectionService
) : SettingsViewModelBase(dataCollectionService, gen24Service, gen24JsonService, fritzBoxService, wattPilotService)
{
    private static readonly IEnumerable<ChargingRuleType> ruleTypes = Enum.GetValues<ChargingRuleType>();

    private Gen24BatterySettings oldSettings = null!;
    private BindableCollection<Gen24ChargingRule> oldChargingRules = null!;

#pragma warning disable CA1822 // Mark members as static
    public IEnumerable<ChargingRuleType> RuleTypes => ruleTypes;
#pragma warning restore CA1822 // Mark members as static

public Gen24BatterySettings Settings
{
    get;
    set => Set(ref field, value);
} = null!;

public bool EnableDanger
{
    get;
    set => Set(ref field, value, () =>
    {
        if (!value)
        {
            Settings.IsEnabled = oldSettings.IsEnabled;
            Settings.IsInCalibration = oldSettings.IsInCalibration;
            Settings.IsAcCoupled = oldSettings.IsAcCoupled;
        }
    });
}

public bool IsSecondary => ReferenceEquals(Gen24Service, DataCollectionService.Gen24Service2);

    public Gen24Storage? Storage => IsSecondary ? DataCollectionService.HomeAutomationSystem?.Gen24Sensors2?.Storage : DataCollectionService.HomeAutomationSystem?.Gen24Sensors?.Storage;

    public string Title
    {
        get;
        set => Set(ref field, value);
    } = Loc.EnergyFlow;

    public double LogGridPower
    {
        get;
        set => Set(ref field, value, UpdateGridPower);
    }

    public double LogHomePower
    {
        get;
        set => Set(ref field, value, UpdateHomePower);
    }

    public int? BatteryAcChargingMaxPower
    {
        get;
        set => Set(ref field, value, UpdateLogHomePower);
    }

    public int? RequestedGridPower
    {
        get;
        set => Set(ref field, value, UpdateLogGridPower);
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

    public BindableCollection<Gen24ChargingRule> ChargingRules
    {
        get;
        set => Set(ref field, value);
    } = null!;

    [field: AllowNull, MaybeNull]
    public ICommand UndoCommand => field ??= new NoParameterCommand(Undo);

    [field: AllowNull, MaybeNull]
    public ICommand ApplyCommand => field ??= new NoParameterCommand(Apply);

    [field: AllowNull, MaybeNull]
    public ICommand DeleteChargingRuleCommand => field ??= new Command<Gen24ChargingRule>(DeleteChargingRule!);

    [field: AllowNull, MaybeNull]
    public ICommand AddChargingRuleCommand => field ??= new NoParameterCommand(AddChargingRule);

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    internal override async Task OnInitialize()
    {
        IsInUpdate = true;

        DataCollectionService.NewDataReceived += OnDataCollectionServiceOnNewDataReceived;

        try
        {
            await base.OnInitialize().ConfigureAwait(false);
            using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            IDictionary<string, Version> softwareVersions;
            JToken configToken;

            try
            {
                configToken = (await Gen24Service.GetFroniusJsonResponse("api/config/", token: tokenSource.Token).ConfigureAwait(false)).Token;
                softwareVersions = Gen24Versions.Parse((await Gen24Service.GetFroniusJsonResponse("api/status/version", token: tokenSource.Token).ConfigureAwait(false)).Token).SwVersions;
            }
            catch (Exception ex)
            {
                IsInUpdate = false;

                ShowBox
                (
                    string.Format(Loc.InverterCommReadError, ex is TaskCanceledException ? Loc.InverterTimeout : ex.Message),
                    ex.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error
                );

                Close();
                return;
            }

            var inverterSettings = Gen24InverterSettings.Parse(configToken);
            oldSettings = Gen24JsonService.ReadFroniusData<Gen24BatterySettings>(configToken["batteries"]?["batteries"]);

            if (!string.IsNullOrWhiteSpace(inverterSettings.SystemName))
            {
                Title = $"{Loc.EnergyFlow} - {inverterSettings.SystemName}";
            }

            if (!softwareVersions.TryGetValue("GEN24", out var value))
            {
                ShowBox(Loc.NoGen24Symo, Loc.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            if (value == new Version(1, 19, 7, 1))
            {
                IsInUpdate = false;
                ShowBox(Loc.UtcBug, Loc.Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
                IsInUpdate = true;
            }

            oldChargingRules = Gen24ChargingRule.Parse(configToken["timeofuse"], Ctx);
            Undo();
        }
        finally
        {
            IsInUpdate = false;
        }

        return;

        void OnDataCollectionServiceOnNewDataReceived(object? s, SolarDataEventArgs e)
        {
            NotifyOfPropertyChange(nameof(Storage));

            if (Storage != null)
            {
                DataCollectionService.NewDataReceived -= OnDataCollectionServiceOnNewDataReceived;
            }
        }
    }

    private void UpdateGridPower()
    {
        RequestedGridPower = (int)Math.Round(Math.Pow(10, LogGridPower), MidpointRounding.AwayFromZero);
        Settings.RequestedGridPower = RequestedGridPower * (IsFeedIn ? -1 : 1);
    }

    private void UpdateHomePower()
    {
        BatteryAcChargingMaxPower = Settings.BatteryAcChargingMaxPower = -(int)Math.Round(Math.Pow(10, LogHomePower), MidpointRounding.AwayFromZero);
    }

    private void UpdateLogHomePower()
    {
        Settings.BatteryAcChargingMaxPower = BatteryAcChargingMaxPower;
        LogHomePower = Math.Log10(-BatteryAcChargingMaxPower ?? double.NegativeInfinity);
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
        Dispatcher.Invoke(() => { ChargingRules = new BindableCollection<Gen24ChargingRule>(((BindableCollection<Gen24ChargingRule>)oldChargingRules.Clone()).OrderBy(r => r.StartTime).ThenBy(r => r.EndTime), SynchronizationContext.Current); });

        Settings = (Gen24BatterySettings)oldSettings.Clone();
        socMin = Settings.SocMin ?? 5;
        socMax = Settings.SocMax ?? 100;
        isFeedIn = Settings.RequestedGridPower < 0;
        NotifyOfPropertyChange(nameof(IsFeedIn));
        NotifyOfPropertyChange(nameof(SocMin));
        NotifyOfPropertyChange(nameof(SocMax));
        LogGridPower = Math.Log10(Math.Abs(Settings.RequestedGridPower ?? .0000001));
        LogHomePower = Math.Log10(Math.Abs(-Settings.BatteryAcChargingMaxPower ?? .0000001));
        NotifyOfPropertyChange(nameof(RequestedGridPower));
        NotifyOfPropertyChange(nameof(BatteryAcChargingMaxPower));
    }

    private async void Apply()
    {
        IsInUpdate = true;

        try
        {
            foreach (var error in NotifiedValidationErrors)
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

            var errorList = new List<string>();

            for (var i = 0; i < ChargingRules.Count; i++)
            {
                if (ChargingRules[i].StartTimeDate > ChargingRules[i].EndTimeDate)
                {
                    errorList.Add(string.Format(Loc.EndBeforeStart, ChargingRules[i]));
                }

                if (i < ChargingRules.Count - 1)
                {
                    for (var j = i + 1; j < ChargingRules.Count; j++)
                    {
                        if (ChargingRules[i].ConflictsWith(ChargingRules[j]))
                        {
                            errorList.Add(string.Format(Loc.ChargingRuleConflict, ChargingRules[i], ChargingRules[j]));
                        }
                    }
                }
            }

            if (errorList.Count > 0)
            {
                ShowBox
                (
                    $"{Loc.PleaseCorrectErrors}:{Environment.NewLine}{errorList.Aggregate(string.Empty, (c, n) => c + Environment.NewLine + "• " + n)}",
                    Loc.Error, MessageBoxButton.OK, MessageBoxImage.Error
                );

                return;
            }

            var rulesChanged = !ChargingRules.SequenceEqual(oldChargingRules);
            var updateToken = Gen24JsonService.GetUpdateToken(Settings, oldSettings);
            var nonRulesChanged = updateToken.Children().Any();
            var rulesToken = Gen24ChargingRule.GetToken(ChargingRules);

            if (!updateToken.Children().Any() && !rulesChanged)
            {
                ShowNoSettingsChanged();
                return;
            }

            if (nonRulesChanged)
            {
                if (!await UpdateInverter("api/config/batteries", updateToken).ConfigureAwait(false))
                {
                    return;
                }
            }

            if (rulesChanged)
            {
                if (!await UpdateInverter("api/config/timeofuse", rulesToken).ConfigureAwait(false))
                {
                    return;
                }
            }

            oldSettings = Settings;
            oldChargingRules = ChargingRules;

            if (Gen24Service == DataCollectionService.Gen24Service && DataCollectionService.HomeAutomationSystem?.Gen24Config != null)
            {
                DataCollectionService.HomeAutomationSystem.Gen24Config.BatterySettings = oldSettings;
            }
            else if (Gen24Service == DataCollectionService.Gen24Service2 && DataCollectionService.HomeAutomationSystem?.Gen24Config2 != null)
            {
                DataCollectionService.HomeAutomationSystem.Gen24Config2.BatterySettings = oldSettings;
            }

            Undo();
            ToastText = Loc.SettingsSavedToInverter;
        }
        finally
        {
            IsInUpdate = false;
        }
    }
}
