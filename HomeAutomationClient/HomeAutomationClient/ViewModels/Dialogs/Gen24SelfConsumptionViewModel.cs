using System.Collections.ObjectModel;
using System.ComponentModel;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Gen24.Settings;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

/// <summary>
/// The energy flow tab of the inverter settings dialog, ported from <c>SelfConsumptionOptimizationViewModel</c>
/// of FroniusMonitor.
/// </summary>
/// <remarks>
/// <para>
/// Two things go to the inverter from here, and they go separately, because the inverter takes them separately:
/// the battery settings as a delta, and the time of use rules as a whole list. Both have their own endpoint on
/// the server, and either can be written without the other - see <see cref="Apply"/>.
/// </para>
/// <para>
/// Every box the user types a number into is a <see cref="string"/> property of this view model with the rule on
/// it, filled from the settings when the tab loads and on Undo, and written back on Apply. That is the text input
/// rule of <c>.claude/rules/ViewModelsForInteractionLogic.md</c>. The sliders beside those boxes are a second
/// view of the same value and are kept in step with them: a slider can only produce a number in range, so it
/// binds to a <see cref="double"/> of its own, and moving it writes the box.
/// </para>
/// <para>
/// The switches that can stop a battery working - enable, calibration, service mode, AC coupling - are behind the
/// danger toggle and are put back to what the inverter holds when it is switched off again, exactly as the WPF
/// dialog does.
/// </para>
/// </remarks>
public sealed partial class Gen24SelfConsumptionViewModel : ViewModelBase
{
    /// <summary>What the log sliders span: a watt at one end, 50 kW at the other.</summary>
    private const double LogPowerMinimum = -0.305d;

    private const double LogPowerMaximum = 4.6989700043360188d;

    private readonly IWebClientService webClient = IoC.GetRegistered<IWebClientService>();
    private readonly ITabHost owner;
    private readonly string deviceId;

    private Gen24BatterySettings loadedSettings;
    private IReadOnlyList<Gen24ChargingRule> loadedRules;
    private bool isLoading;
    private bool isSyncing;

    public Gen24SelfConsumptionViewModel
    (
        ITabHost owner,
        string deviceId,
        Gen24BatterySettings settings,
        IEnumerable<Gen24ChargingRule> chargingRules
    )
    {
        this.owner = owner;
        this.deviceId = deviceId;
        loadedSettings = settings;
        loadedRules = [.. chargingRules];
        Settings = settings;

        // A battery that may be charged from the grid is charged from the house as well; the inverter does not
        // always say so, and the two combo box entries would then both be wrong. FroniusMonitor fixes it up the
        // same way, on what it read from the inverter.
        if (loadedSettings.ChargeFromGrid is true)
        {
            loadedSettings.ChargeFromAc = true;
        }

        Reset();
    }

    /// <inheritdoc cref="Gen24ModbusViewModel.BusyText"/>
    public override string? BusyText
    {
        get => owner.BusyText;
        set => owner.BusyText = value;
    }

    /// <inheritdoc cref="Gen24ModbusViewModel.ToastText"/>
    public string? ToastText
    {
        get => owner.ToastText;
        set => owner.ToastText = value;
    }

    public IReadOnlyList<EnumListItemModel<ChargingRuleType>> RuleTypes { get; } =
        [.. Enum.GetValues<ChargingRuleType>().Select(t => new EnumListItemModel<ChargingRuleType> { Value = t })];

    public IReadOnlyList<EnumListItemModel<Gen24ChargingSources>> ChargingSources { get; } =
        [.. Enum.GetValues<Gen24ChargingSources>().Select(s => new EnumListItemModel<Gen24ChargingSources> { Value = s })];

    [ObservableProperty]
    public partial Gen24BatterySettings Settings { get; set; }

    public ObservableCollection<Gen24ChargingRuleViewModel> ChargingRules { get; } = [];

    /// <summary>
    /// Where the state of charge sliders start and end. The battery says what it can take - a nameplate minimum
    /// of 5% and a maximum of 100% is usual - and the inverter will not go outside that, so neither do the
    /// sliders. Without a live battery to ask, the whole range.
    /// </summary>
    public double SocMinimum { get; private set; }

    /// <inheritdoc cref="SocMinimum"/>
    public double SocMaximum { get; private set; } = 100d;

    /// <summary>
    /// The switches that can stop a battery working stay disabled until the user says they mean it, and go back
    /// to what the inverter holds when it is switched off again.
    /// </summary>
    [ObservableProperty]
    public partial bool EnableDanger { get; set; }

    #region Grid power management

    /// <summary>Whether the inverter follows its own rules or the number below.</summary>
    public bool IsManualMode => Settings.Mode is OptimizationMode.Manual;

    /// <summary>
    /// Whether the number below means power fed into the grid rather than taken from it. The inverter keeps one
    /// signed number for both, negative for feed in.
    /// </summary>
    [ObservableProperty]
    public partial bool IsFeedIn { get; set; }

    /// <summary>How much the inverter should feed in or take from the grid, as the box holds it.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MinMaxInt(0, 50000, AllowEmpty = false)]
    public partial string? RequestedGridPowerText { get; set; }

    /// <summary>
    /// The same number on a slider, logarithmically: a watt at one end and 50 kW at the other, because the
    /// interesting settings are all at the bottom of that range.
    /// </summary>
    [ObservableProperty]
    public partial double LogGridPower { get; set; }

    #endregion

    #region State of charge

    /// <summary>Whether the inverter uses the limits of the battery or the two below.</summary>
    public bool ShowSocLimits => Settings.Limits is SocLimits.Override;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MinMaxInt(0, 100, AllowEmpty = false)]
    public partial string? SocMinText { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MinMaxInt(0, 100, AllowEmpty = false)]
    public partial string? SocMaxText { get; set; }

    /// <summary>
    /// The lower limit on its slider. Pushing it above the upper limit takes the upper limit with it, which is
    /// what the WPF dialog does: the two cannot cross, and refusing the drag would be a worse way to say so.
    /// </summary>
    [ObservableProperty]
    public partial double SocMin { get; set; }

    /// <inheritdoc cref="SocMin"/>
    [ObservableProperty]
    public partial double SocMax { get; set; }

    #endregion

    #region Support the OhmPilot

    public bool ShowSupportSoc => Settings.EnableSupportSoc is true;

    public bool ShowSupportSocPercent => ShowSupportSoc && Settings.SupportSocMode is SocLimits.Override;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MinMaxInt(0, 100, AllowEmpty = false)]
    public partial string? SupportSocPercentText { get; set; }

    [ObservableProperty]
    public partial double SupportSocPercent { get; set; }

    /// <summary>The hysteresis in watt hours. The inverter keeps it in joules; the model converts.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MinMaxDouble(0, 5000, AllowEmpty = false, PropertyDisplayNameResourceKey = nameof(Loc.EnergyHysteresis))]
    public partial string? SupportSocHysteresisText { get; set; }

    [ObservableProperty]
    public partial double SupportSocHysteresis { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MinMaxInt(0, 100, AllowEmpty = false, PropertyDisplayNameResourceKey = nameof(Loc.SupportSocHysteresisMinimum))]
    public partial string? SupportSocHysteresisMinimumText { get; set; }

    [ObservableProperty]
    public partial double SupportSocHysteresisMinimum { get; set; }

    #endregion

    #region Battery charging options

    /// <summary>
    /// Where the battery may be charged from. Two flags of the inverter in one list, because only three of their
    /// four combinations mean anything: the grid without the house is not one the inverter offers.
    /// </summary>
    [ObservableProperty]
    public partial Gen24ChargingSources SelectedChargingSources { get; set; }

    public bool ShowAcChargingPower => Settings.ChargeFromAc is true;

    /// <summary>
    /// The most the battery may be charged with from the house, as the box holds it. Positive here; the inverter
    /// keeps it as a negative number, because to the battery it is power flowing the other way.
    /// </summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MinMaxInt(0, 50000, AllowEmpty = false)]
    public partial string? BatteryAcChargingMaxPowerText { get; set; }

    /// <inheritdoc cref="LogGridPower"/>
    [ObservableProperty]
    public partial double LogHomePower { get; set; }

    #endregion

    #region Backup

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MinMaxInt(0, 100, AllowEmpty = false, MessageResourceKey = nameof(Loc.MustBePercent))]
    public partial string? BackupCriticalSocText { get; set; }

    [ObservableProperty]
    public partial double BackupCriticalSoc { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MinMaxInt(0, 100, AllowEmpty = false, MessageResourceKey = nameof(Loc.MustBePercent))]
    public partial string? BackupReserveText { get; set; }

    [ObservableProperty]
    public partial double BackupReserve { get; set; }

    #endregion

    /// <summary>
    /// Takes the limits of the battery from the live device, where there is one. The dialog knows the inverter by
    /// its key, and everything the client knows about it comes through <see cref="IUpdateService"/>.
    /// </summary>
    public void ReadBatteryLimits()
    {
        if (IoC.TryGetRegistered<IUpdateService>()?.Inverters.FirstOrDefault(i => i.Key == deviceId)?.Device.Sensors?.Storage is not { } storage)
        {
            return;
        }

        SocMinimum = storage.MinimumStateOfCharge * 100d;
        SocMaximum = storage.MaximumStateOfCharge * 100d;
        OnPropertyChanged(nameof(SocMinimum));
        OnPropertyChanged(nameof(SocMaximum));
    }

    [RelayCommand]
    private void AddChargingRule() => ChargingRules.Add(new Gen24ChargingRuleViewModel(this, Gen24ChargingRuleViewModel.NewRule()));

    public void Remove(Gen24ChargingRuleViewModel rule) => ChargingRules.Remove(rule);

    /// <inheritdoc cref="Gen24ModbusViewModel.Undo"/>
    [RelayCommand]
    private void Undo()
    {
        ToastText = null;
        Reset();
    }

    [RelayCommand]
    private Task Apply() => TaskExceptionHandler(async () =>
    {
        ToastText = null;

        IReadOnlyList<string> errors = Problems();

        if (errors.Count > 0)
        {
            await new MessageBox
            {
                Text = $"{Loc.PleaseCorrectErrors}:",
                ItemList = [.. errors],
                Title = Loc.Error,
                Buttons = [Loc.Ok],
                Icon = new ErrorIcon(),
            }.Show().ConfigureAwait(true);

            return;
        }

        CopyToSettings();
        ChargingRules.Apply(rule => rule.CopyToRule());

        var wantedRules = ChargingRules.Select(rule => rule.Rule).ToList();
        var settingsChanged = IoC.Get<IGen24JsonService>().GetUpdateToken(Settings, loadedSettings).HasValues();
        var rulesChanged = !wantedRules.SequenceEqual(loadedRules);

        if (!settingsChanged && !rulesChanged)
        {
            await new MessageBox
            {
                Text = Loc.NoSettingsChanged,
                Title = Loc.Warning,
                Buttons = [Loc.Ok],
                Icon = new WarningIcon(),
            }.Show().ConfigureAwait(true);

            return;
        }

        BusyText = string.Format(CultureInfo.CurrentCulture, Loc.SavingSettings, Loc.EnergyFlow);

        // Two writes, because the inverter has two endpoints for them and either can be the only one that
        // changed. The battery settings go first: they are a delta, so a failure there leaves the inverter as it
        // was, and stopping before the rules keeps the two from being half applied in the other order.
        if (settingsChanged)
        {
            var result = await webClient.SetGen24BatterySettings(deviceId, Settings).ConfigureAwait(true);

            if (result.Status != HttpStatusCode.OK)
            {
                await ShowHttpError(result).ConfigureAwait(true);
                return;
            }
        }

        if (rulesChanged)
        {
            var result = await webClient.SetGen24TimeOfUse(deviceId, wantedRules).ConfigureAwait(true);

            if (result.Status != HttpStatusCode.OK)
            {
                await ShowHttpError(result).ConfigureAwait(true);
                return;
            }
        }

        // What was just written is what Undo goes back to from now on.
        loadedSettings = (Gen24BatterySettings)Settings.Clone();
        loadedRules = [.. wantedRules.Select(rule => (Gen24ChargingRule)rule.Clone())];
        Reset();

        ToastText = Loc.SettingsSavedToInverter;
    });

    /// <summary>
    /// Everything the user would have to put right before this can be saved, in the words they get to read:
    /// what a single field can see for itself, and what only the whole schedule can.
    /// </summary>
    public IReadOnlyList<string> Problems() => [.. FieldErrors().Concat(RuleErrors())];

    /// <summary>Whatever the boxes and the settings have been told is wrong, in the words the user gets to read.</summary>
    private IEnumerable<string> FieldErrors() => Settings.GetErrors()
        .Concat(GetErrors())
        .Concat(ChargingRules.SelectMany(rule => rule.GetErrors()))
        .Select(error => error.ErrorMessage)
        .Where(message => !string.IsNullOrWhiteSpace(message))
        .Select(message => message!)
        .Distinct();

    /// <summary>
    /// What no single field can see: a rule that ends before it starts, and two rules that contradict each other.
    /// </summary>
    /// <remarks>
    /// Only worth asking once every box holds something sensible - a rule whose time is refused has no times to
    /// compare - so the times are read from the boxes and a rule that cannot be read is left to its own message.
    /// </remarks>
    private IEnumerable<string> RuleErrors()
    {
        var rules = ChargingRules.Where(rule => !rule.HasErrors).ToList();
        rules.Apply(rule => rule.CopyToRule());

        for (var i = 0; i < rules.Count; i++)
        {
            if (rules[i].Rule.StartTimeDate > rules[i].Rule.EndTimeDate)
            {
                yield return string.Format(CultureInfo.CurrentCulture, Loc.EndBeforeStart, rules[i]);
            }

            for (var j = i + 1; j < rules.Count; j++)
            {
                if (rules[i].Rule.ConflictsWith(rules[j].Rule))
                {
                    yield return string.Format(CultureInfo.CurrentCulture, Loc.ChargingRuleConflict, rules[i], rules[j]);
                }
            }
        }
    }

    /// <inheritdoc cref="Gen24ModbusViewModel.Reset"/>
    private void Reset()
    {
        isLoading = true;

        try
        {
            DetachFrom(Settings);
            Settings = (Gen24BatterySettings)loadedSettings.Clone();
            AttachTo(Settings);

            ChargingRules.Clear();
            loadedRules
                .OrderBy(rule => rule.StartTime, StringComparer.Ordinal)
                .ThenBy(rule => rule.EndTime, StringComparer.Ordinal)
                .Apply(rule => ChargingRules.Add(new Gen24ChargingRuleViewModel(this, rule)));

            CopyFromSettings();
        }
        finally
        {
            isLoading = false;
        }
    }
    /// <summary>The numbers of the settings into the strings the boxes are bound to, and onto the sliders.</summary>
    /// <remarks>
    /// Both halves of every pair are written here rather than left to the change handlers below. A handler is
    /// there to keep a box and its slider in step while the user works; on a load it would write whichever half
    /// happened to be set first and the other would keep what it had - which is how the boxes came out empty on
    /// the first attempt at this.
    /// </remarks>
    private void CopyFromSettings()
    {
        IsFeedIn = Settings.RequestedGridPower < 0;
        SetPair(Math.Abs(Settings.RequestedGridPower ?? 0), text => RequestedGridPowerText = text, watts => LogGridPower = Log((int)watts));
        SetPair(Math.Abs(Settings.BatteryAcChargingMaxPower ?? 0), text => BatteryAcChargingMaxPowerText = text, watts => LogHomePower = Log((int)watts));

        SetPair(Settings.SocMin ?? 5, text => SocMinText = text, value => SocMin = value);
        SetPair(Settings.SocMax ?? 100, text => SocMaxText = text, value => SocMax = value);
        SetPair(Settings.SupportSocPercent ?? 0, text => SupportSocPercentText = text, value => SupportSocPercent = value);
        SetPair((int)Math.Round(Settings.SupportSocHysteresisWatts ?? 0), text => SupportSocHysteresisText = text, value => SupportSocHysteresis = value);
        SetPair(Settings.SupportSocHysteresisMinimumPercent ?? 0, text => SupportSocHysteresisMinimumText = text, value => SupportSocHysteresisMinimum = value);
        SetPair(Settings.BackupCriticalSoc ?? 0, text => BackupCriticalSocText = text, value => BackupCriticalSoc = value);
        SetPair(Settings.BackupReserve ?? 0, text => BackupReserveText = text, value => BackupReserve = value);

        SelectedChargingSources = Settings switch
        {
            { ChargeFromGrid: true } => Gen24ChargingSources.HomeAndGrid,
            { ChargeFromAc: true } => Gen24ChargingSources.Home,
            _ => Gen24ChargingSources.InverterPv,
        };

        NotifyAllVisibilities();

        // Explicitly: a setter validates only what it actually stores, so writing the same value twice - at load,
        // or after Undo - would leave whatever error was there standing.
        ValidateAllProperties();
    }

    /// <summary>One number into the box that shows it and onto the slider beside it, both of them.</summary>
    private void SetPair(int value, Action<string> writeBox, Action<double> moveSlider) => Guard(() =>
    {
        writeBox(Text(value));
        moveSlider(value);
    });

    /// <summary>And back again, on the way to the inverter. Only ever called once the rules have passed.</summary>
    public void CopyToSettings()
    {
        Settings.RequestedGridPower = (int?)NumericText.ToInteger(RequestedGridPowerText) * (IsFeedIn ? -1 : 1);
        Settings.BatteryAcChargingMaxPower = -(int?)NumericText.ToInteger(BatteryAcChargingMaxPowerText);
        Settings.SocMin = (byte?)NumericText.ToInteger(SocMinText);
        Settings.SocMax = (byte?)NumericText.ToInteger(SocMaxText);
        Settings.SupportSocPercent = (byte?)NumericText.ToInteger(SupportSocPercentText);
        Settings.SupportSocHysteresisWatts = NumericText.ToNumber(SupportSocHysteresisText);
        Settings.SupportSocHysteresisMinimumPercent = (byte?)NumericText.ToInteger(SupportSocHysteresisMinimumText);
        Settings.BackupCriticalSoc = (byte?)NumericText.ToInteger(BackupCriticalSocText);
        Settings.BackupReserve = (byte?)NumericText.ToInteger(BackupReserveText);
    }

    #region Keeping a box and its slider in step

    /// <summary>
    /// Whichever of the two the user moved, the other follows. One guard stops the pair chasing each other: the
    /// first of them to be written does the writing, and the change it causes in the other is ignored.
    /// </summary>
    private void Guard(Action write)
    {
        if (isSyncing)
        {
            return;
        }

        isSyncing = true;

        try
        {
            write();
        }
        finally
        {
            isSyncing = false;
        }
    }

    /// <summary>
    /// What the user typed, as a number, or nothing where the rule refuses it. Text a rule refuses leaves the
    /// slider where it was: a half typed number is not a position, and a slider that jumped about while the box
    /// was being filled in would be worse than one that waits.
    /// </summary>
    private double? Number(string? text, string propertyName) =>
        !GetErrors(propertyName).Any() && NumericText.TryParseNumber(text, out var value) ? value : null;

    private static string Text(double value) => NumericText.Of((int)Math.Round(value))!;

    /// <summary>
    /// A power on its logarithmic slider. Zero has no logarithm and the slider has to sit somewhere, so it sits
    /// at the end of its travel.
    /// </summary>
    private static double Log(int watts) => watts <= 0 ? LogPowerMinimum : Math.Clamp(Math.Log10(watts), LogPowerMinimum, LogPowerMaximum);

    private static int FromLog(double log) => (int)Math.Round(Math.Pow(10, log), MidpointRounding.AwayFromZero);

    partial void OnRequestedGridPowerTextChanged(string? value) =>
        Guard(() => { if (Number(value, nameof(RequestedGridPowerText)) is { } watts) { LogGridPower = Log((int)watts); } });

    partial void OnLogGridPowerChanged(double value) => Guard(() => RequestedGridPowerText = Text(FromLog(value)));

    partial void OnBatteryAcChargingMaxPowerTextChanged(string? value) =>
        Guard(() => { if (Number(value, nameof(BatteryAcChargingMaxPowerText)) is { } watts) { LogHomePower = Log((int)watts); } });

    partial void OnLogHomePowerChanged(double value) => Guard(() => BatteryAcChargingMaxPowerText = Text(FromLog(value)));

    partial void OnSupportSocPercentTextChanged(string? value) =>
        Guard(() => { if (Number(value, nameof(SupportSocPercentText)) is { } percent) { SupportSocPercent = percent; } });

    partial void OnSupportSocPercentChanged(double value) => Guard(() => SupportSocPercentText = Text(value));

    partial void OnSupportSocHysteresisTextChanged(string? value) =>
        Guard(() => { if (Number(value, nameof(SupportSocHysteresisText)) is { } watts) { SupportSocHysteresis = watts; } });

    partial void OnSupportSocHysteresisChanged(double value) => Guard(() => SupportSocHysteresisText = Text(value));

    partial void OnSupportSocHysteresisMinimumTextChanged(string? value) =>
        Guard(() => { if (Number(value, nameof(SupportSocHysteresisMinimumText)) is { } percent) { SupportSocHysteresisMinimum = percent; } });

    partial void OnSupportSocHysteresisMinimumChanged(double value) => Guard(() => SupportSocHysteresisMinimumText = Text(value));

    partial void OnBackupCriticalSocTextChanged(string? value) =>
        Guard(() => { if (Number(value, nameof(BackupCriticalSocText)) is { } percent) { BackupCriticalSoc = percent; } });

    partial void OnBackupCriticalSocChanged(double value) => Guard(() => BackupCriticalSocText = Text(value));

    partial void OnBackupReserveTextChanged(string? value) =>
        Guard(() => { if (Number(value, nameof(BackupReserveText)) is { } percent) { BackupReserve = percent; } });

    partial void OnBackupReserveChanged(double value) => Guard(() => BackupReserveText = Text(value));

    partial void OnSocMinTextChanged(string? value) =>
        Guard(() => { if (Number(value, nameof(SocMinText)) is { } percent) { SocMin = percent; KeepSocLimitsApart(percent, isMinimum: true); } });

    partial void OnSocMaxTextChanged(string? value) =>
        Guard(() => { if (Number(value, nameof(SocMaxText)) is { } percent) { SocMax = percent; KeepSocLimitsApart(percent, isMinimum: false); } });

    partial void OnSocMinChanged(double value) => Guard(() =>
    {
        SocMinText = Text(value);
        KeepSocLimitsApart(value, isMinimum: true);
    });

    partial void OnSocMaxChanged(double value) => Guard(() =>
    {
        SocMaxText = Text(value);
        KeepSocLimitsApart(value, isMinimum: false);
    });

    /// <summary>
    /// The two state of charge limits may not cross, so the one that is in the way comes along. Refusing the drag
    /// would be a worse way of saying so, and taking the other one along is what the WPF dialog does.
    /// </summary>
    /// <remarks>
    /// Called from inside the guard, so the other half of the pair that is moved has to be written here as well:
    /// the change it raises finds the guard closed.
    /// </remarks>
    private void KeepSocLimitsApart(double value, bool isMinimum)
    {
        if (isMinimum && SocMax < value)
        {
            SocMax = value;
            SocMaxText = Text(value);
        }
        else if (!isMinimum && SocMin > value)
        {
            SocMin = value;
            SocMinText = Text(value);
        }
    }

    #endregion

    partial void OnSelectedChargingSourcesChanged(Gen24ChargingSources value)
    {
        (Settings.ChargeFromAc, Settings.ChargeFromGrid) = value switch
        {
            Gen24ChargingSources.HomeAndGrid => (true, true),
            Gen24ChargingSources.Home => (true, false),
            _ => (false, false),
        };
    }

    partial void OnEnableDangerChanged(bool value)
    {
        if (value)
        {
            return;
        }

        // Off again: whatever the user did with them goes back to what the inverter holds.
        Settings.IsEnabled = loadedSettings.IsEnabled;
        Settings.IsInCalibration = loadedSettings.IsInCalibration;
        Settings.IsInServiceMode = loadedSettings.IsInServiceMode;
        Settings.IsAcCoupled = loadedSettings.IsAcCoupled;
    }

    private void AttachTo(Gen24BatterySettings settings) => settings.PropertyChanged += OnSettingsPropertyChanged;

    private void DetachFrom(Gen24BatterySettings settings) => settings.PropertyChanged -= OnSettingsPropertyChanged;

    /// <summary>
    /// The visibility rules read properties of <see cref="Settings"/>, and a change there is a change of this view
    /// model as far as the view is concerned.
    /// </summary>
    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!isLoading)
        {
            ToastText = null;
        }

        switch (e.PropertyName)
        {
            case nameof(Gen24BatterySettings.Mode):
                OnPropertyChanged(nameof(IsManualMode));
                break;

            case nameof(Gen24BatterySettings.Limits):
                OnPropertyChanged(nameof(ShowSocLimits));
                break;

            case nameof(Gen24BatterySettings.EnableSupportSoc):
            case nameof(Gen24BatterySettings.SupportSocMode):
                OnPropertyChanged(nameof(ShowSupportSoc));
                OnPropertyChanged(nameof(ShowSupportSocPercent));
                break;

            case nameof(Gen24BatterySettings.ChargeFromAc):
                OnPropertyChanged(nameof(ShowAcChargingPower));
                break;
        }
    }

    private void NotifyAllVisibilities()
    {
        OnPropertyChanged(nameof(IsManualMode));
        OnPropertyChanged(nameof(ShowSocLimits));
        OnPropertyChanged(nameof(ShowSupportSoc));
        OnPropertyChanged(nameof(ShowSupportSocPercent));
        OnPropertyChanged(nameof(ShowAcChargingPower));
    }
}
