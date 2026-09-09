using System.ComponentModel;
using System.Reflection;
using De.Hochstaetter.Fronius.Attributes;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.HomeAutomationClient;
using De.Hochstaetter.Fronius.Models.Gen24.Settings;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

/// <summary>
/// The inverter settings tab of the settings dialog, ported from <c>InverterSettingsViewModel</c> of
/// FroniusMonitor. Three groups: what the system is called and how it keeps time, the string trackers, and the
/// limit on what may be fed into the grid.
/// </summary>
/// <remarks>
/// <para>
/// Three things go to the inverter from here and they go separately, because the inverter keeps them in three
/// places: <c>api/config/common</c>, <c>api/config/powerunit</c> and
/// <c>api/config/limit_settings/powerLimits</c>. Each has an endpoint of its own on the server and
/// <see cref="Apply"/> sends only the ones that actually changed - worked out here with the same delta the server
/// will work out again against what the inverter holds by then.
/// </para>
/// <para>
/// The groups the inverter reserves for a technician - the trackers and the export limits - are only shown where
/// the server is logged in to the inverter as one. The WPF app asked its own connection for that; here it comes
/// with the snapshot, see <see cref="Gen24SettingsSnapshot.IsInverterTechnician"/>.
/// </para>
/// <para>
/// The time zone and the clock are behind the danger switch, as they are in the WPF dialog: an inverter whose
/// clock is wrong times its charging rules wrong, and the rules are what the energy flow tab is full of.
/// </para>
/// </remarks>
public sealed partial class Gen24InverterSettingsViewModel : ViewModelBase
{
    private readonly IWebClientService webClient = IoC.GetRegistered<IWebClientService>();
    private readonly IGen24LocalizationService gen24Loc = IoC.GetRegistered<IGen24LocalizationService>();
    private readonly ITabHost owner;
    private readonly string deviceId;

    private Gen24InverterSettings loadedSettings;
    private bool isLoading;
    private bool isSyncing;

    public Gen24InverterSettingsViewModel(ITabHost owner, string deviceId, Gen24InverterSettings settings, bool isInverterTechnician)
    {
        this.owner = owner;
        this.deviceId = deviceId;
        loadedSettings = settings;
        Settings = settings;
        IsInverterTechnician = isInverterTechnician;

        // The words of the inverter, not ours: these are its own modes and it has names for them in the language
        // of the user. Read once here rather than per combo box entry.
        PowerModes = [.. new[] { MpptPowerMode.Off, MpptPowerMode.Auto, MpptPowerMode.Fix }.Select(FroniusItem)];
        DynamicPeakManagerModes = [.. new[] { MpptOnOff.Off, MpptOnOff.On, MpptOnOff.OnMlsd }.Select(FroniusItem)];

        PhaseModes =
        [
            new ListItemModel<PhaseMode> { Value = PhaseMode.PhaseSum, DisplayName = Config("EXPORTLIMIT.WLIM_MAX_W") },
            new ListItemModel<PhaseMode> { Value = PhaseMode.WeakestPhase, DisplayName = Config("EXPORTLIMIT.WLIM_MAX_FEEDIN_PER_PHASE") },
        ];

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

    [ObservableProperty]
    public partial Gen24InverterSettings Settings { get; set; }

    /// <summary>
    /// Whether the server may change what the inverter only lets a technician change. Where it may not, the
    /// trackers and the export limits are not shown at all rather than offered and then refused.
    /// </summary>
    public bool IsInverterTechnician { get; }

    /// <inheritdoc cref="Gen24SelfConsumptionViewModel.EnableDanger"/>
    [ObservableProperty]
    public partial bool EnableDanger { get; set; }

    public IReadOnlyList<ListItemModel<MpptPowerMode>> PowerModes { get; }

    public IReadOnlyList<ListItemModel<MpptOnOff>> DynamicPeakManagerModes { get; }

    public IReadOnlyList<ListItemModel<PhaseMode>> PhaseModes { get; }

    #region The common settings

    /// <summary>
    /// What the system is called, straight on the settings. A name is text in its own right - there is nothing
    /// for a binding to convert and nothing it can refuse - so the box binds to it and no proxy is needed.
    /// </summary>
    public const int NameMaximumLength = 30;

    #endregion

    #region The string trackers

    /// <summary>
    /// The trackers of this inverter, one view model each, and none at all where the inverter reports none. An
    /// inverter with a single string has one, and the second group is then simply not there.
    /// </summary>
    [ObservableProperty]
    public partial Gen24MpptViewModel? Mppt1 { get; set; }

    /// <inheritdoc cref="Mppt1"/>
    [ObservableProperty]
    public partial Gen24MpptViewModel? Mppt2 { get; set; }

    public bool ShowMppt1 => IsInverterTechnician && Mppt1 is not null;

    public bool ShowMppt2 => IsInverterTechnician && Mppt2 is not null;

    #endregion

    #region The export limits

    public bool ShowExportLimits => IsInverterTechnician;

    /// <summary>Everything below the switch is only there while there is a limit at all.</summary>
    public bool ShowExportLimitDetails => Limits.IsEnabled;

    public bool ShowHardLimit => Limits.HardLimit.IsEnabled;

    public bool ShowSoftLimit => Limits.SoftLimit.IsEnabled;

    /// <summary>
    /// The peak power the limits are read against, as the box holds it. Not a limit itself: it is what the
    /// inverter's own display divides a limit by to show it as a percentage.
    /// </summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MinMaxDouble(0, 200_000, AllowEmpty = false, PropertyDisplayNameResourceKey = nameof(Loc.Power))]
    public partial string? WattPeakReferenceText { get; set; }

    /// <inheritdoc cref="WattPeakReferenceText"/>
    [ObservableProperty]
    public partial double WattPeakReference { get; set; }

    /// <summary>The limit the inverter must never exceed, as the box holds it.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MinMaxDouble(0, 200_000, AllowEmpty = false, PropertyDisplayNameResourceKey = nameof(Loc.Power))]
    public partial string? HardLimitText { get; set; }

    /// <inheritdoc cref="HardLimitText"/>
    [ObservableProperty]
    public partial double HardLimit { get; set; }

    /// <summary>The limit it aims at, as the box holds it. Never above the hard limit.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MinMaxDouble(0, 200_000, AllowEmpty = false, PropertyDisplayNameResourceKey = nameof(Loc.Power))]
    public partial string? SoftLimitText { get; set; }

    /// <inheritdoc cref="SoftLimitText"/>
    [ObservableProperty]
    public partial double SoftLimit { get; set; }

    /// <summary>Where the three power sliders end. The boxes take more; the slider is for the usual range.</summary>
    public double PowerSliderMaximum => 20_000d;

    #endregion

    /// <summary>The export limits of this inverter, which is where most of this tab writes to.</summary>
    private Gen24PowerLimit Limits => Settings.PowerLimitSettings.ExportLimits.ActivePower;

    /// <summary>A string of the inverter in the language of the user - the captions of the trackers use this.</summary>
    internal string Channel(string key) => gen24Loc.GetLocalizedString(Gen24LocalizationSection.Channels, key);

    /// <summary>A tracker changed something of its own, which is a change of this tab.</summary>
    internal void OnTrackerChanged()
    {
        if (!isLoading)
        {
            ToastText = null;
        }
    }

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

        var json = IoC.Get<IGen24JsonService>();
        var commonChanged = json.GetUpdateToken(Settings, loadedSettings).HasAnyValue();
        var mpptChanged = Settings.Mppt is { } mppt && mppt.GetToken(loadedSettings.Mppt).HasAnyValue();
        var limitsChanged = Settings.PowerLimitSettings.GetToken(loadedSettings.PowerLimitSettings).HasAnyValue();

        if (!commonChanged && !mpptChanged && !limitsChanged)
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

        BusyText = string.Format(CultureInfo.CurrentCulture, Loc.SavingSettings, Loc.InverterSettings);

        // Three writes, because the inverter keeps these three groups in three places and any one of them can be
        // the only thing that changed. Each is a delta, so stopping at the first failure leaves the inverter with
        // whatever went before it and nothing half applied inside a group.
        if (commonChanged && !await Write(() => webClient.SetGen24CommonSettings(deviceId, Settings)).ConfigureAwait(true))
        {
            return;
        }

        if (mpptChanged && !await Write(() => webClient.SetGen24MpptSettings(deviceId, Settings.Mppt!)).ConfigureAwait(true))
        {
            return;
        }

        if (limitsChanged && !await Write(() => webClient.SetGen24PowerLimits(deviceId, Settings.PowerLimitSettings)).ConfigureAwait(true))
        {
            return;
        }

        // What was just written is what Undo goes back to from now on.
        loadedSettings = (Gen24InverterSettings)Settings.Clone();
        Reset();

        ToastText = Loc.SettingsSavedToInverter;
    });

    /// <summary>One of the three writes. False where it failed and the user has been told why.</summary>
    private async Task<bool> Write(Func<Task<ApiResult<bool>>> write)
    {
        var result = await write().ConfigureAwait(true);

        if (result.Status == HttpStatusCode.OK)
        {
            return true;
        }

        await ShowHttpError(result).ConfigureAwait(true);
        return false;
    }

    /// <inheritdoc cref="Gen24SelfConsumptionViewModel.Problems"/>
    public IReadOnlyList<string> Problems() =>
    [
        .. Settings.GetErrors()
            .Concat(GetErrors())
            .Concat(Mppt1?.GetErrors() ?? [])
            .Concat(Mppt2?.GetErrors() ?? [])
            .Select(error => error.ErrorMessage)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Select(message => message!)
            .Distinct(),
    ];

    /// <inheritdoc cref="Gen24ModbusViewModel.Reset"/>
    private void Reset()
    {
        isLoading = true;

        try
        {
            DetachFrom(Settings);
            Settings = (Gen24InverterSettings)loadedSettings.Clone();
            AttachTo(Settings);

            Mppt1 = Settings.Mppt?.Mppt1 is { } mppt1 ? new Gen24MpptViewModel(this, mppt1, 1) : null;
            Mppt2 = Settings.Mppt?.Mppt2 is { } mppt2 ? new Gen24MpptViewModel(this, mppt2, 2) : null;

            CopyFromSettings();
        }
        finally
        {
            isLoading = false;
        }
    }

    /// <inheritdoc cref="Gen24SelfConsumptionViewModel.CopyFromSettings"/>
    private void CopyFromSettings()
    {

        SetPair(Settings.PowerLimitSettings.Visualization.WattPeakReferenceValue, text => WattPeakReferenceText = text, value => WattPeakReference = value);
        SetPair(Limits.HardLimit.PowerLimit, text => HardLimitText = text, value => HardLimit = value);
        SetPair(Limits.SoftLimit.PowerLimit, text => SoftLimitText = text, value => SoftLimit = value);

        NotifyAllVisibilities();
        ValidateAllProperties();
    }

    /// <inheritdoc cref="Gen24SelfConsumptionViewModel.SetPair"/>
    private void SetPair(double value, Action<string?> writeBox, Action<double> moveSlider) => Guard(() =>
    {
        writeBox(NumericText.Of(value, "F0"));
        moveSlider(Math.Clamp(value, 0d, PowerSliderMaximum));
    });

    /// <summary>
    /// The boxes back into the settings, on the way to the inverter, with the two rules of the inverter applied:
    /// a limit that is switched off is zero, and a limit that is not switched on at all takes both with it.
    /// </summary>
    /// <remarks>
    /// The WPF app replaces the whole <see cref="Gen24PowerLimitSettings"/> with a new one where limiting is off,
    /// which also puts the peak power reference back to zero. That is not done here: the reference is what the
    /// inverter's display reads limits against and not a limit itself, so switching limiting off is no reason to
    /// forget the size of the system.
    /// </remarks>
    public void CopyToSettings()
    {
        Settings.PowerLimitSettings.Visualization.WattPeakReferenceValue = NumericText.ToNumber(WattPeakReferenceText) ?? 0;
        Limits.HardLimit.PowerLimit = NumericText.ToNumber(HardLimitText) ?? 0;
        Limits.SoftLimit.PowerLimit = NumericText.ToNumber(SoftLimitText) ?? 0;

        if (!Limits.IsEnabled)
        {
            Limits.HardLimit.IsEnabled = false;
            Limits.SoftLimit.IsEnabled = false;
        }

        ZeroWhereDisabled(Limits.HardLimit);
        ZeroWhereDisabled(Limits.SoftLimit);

        Mppt1?.CopyToTracker();
        Mppt2?.CopyToTracker();

        static void ZeroWhereDisabled(Gen24PowerLimitDefinition limit)
        {
            if (!limit.IsEnabled)
            {
                limit.PowerLimit = 0;
            }
        }
    }

    #region Keeping the three limits in order

    /// <inheritdoc cref="Gen24SelfConsumptionViewModel.Guard"/>
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

    /// <inheritdoc cref="Gen24SelfConsumptionViewModel.Number"/>
    private double? Number(string? text, string propertyName) =>
        !GetErrors(propertyName).Any() && NumericText.TryParseNumber(text, out var value) ? value : null;

    partial void OnWattPeakReferenceTextChanged(string? value) => Guard(() =>
    {
        if (Number(value, nameof(WattPeakReferenceText)) is { } watts)
        {
            WattPeakReference = Math.Clamp(watts, 0d, PowerSliderMaximum);
            KeepLimitsUnderTheReference(watts);
        }
    });

    partial void OnWattPeakReferenceChanged(double value) => Guard(() =>
    {
        WattPeakReferenceText = NumericText.Of(Math.Round(value), "F0");
        KeepLimitsUnderTheReference(value);
    });

    partial void OnHardLimitTextChanged(string? value) => Guard(() =>
    {
        if (Number(value, nameof(HardLimitText)) is { } watts)
        {
            HardLimit = Math.Clamp(watts, 0d, PowerSliderMaximum);
            KeepTheSoftLimitUnderTheHardOne(watts);
        }
    });

    partial void OnHardLimitChanged(double value) => Guard(() =>
    {
        HardLimitText = NumericText.Of(Math.Round(value), "F0");
        KeepTheSoftLimitUnderTheHardOne(value);
    });

    partial void OnSoftLimitTextChanged(string? value) => Guard(() =>
    {
        if (Number(value, nameof(SoftLimitText)) is { } watts)
        {
            SoftLimit = Math.Clamp(watts, 0d, PowerSliderMaximum);
            KeepTheHardLimitOverTheSoftOne(watts);
        }
    });

    partial void OnSoftLimitChanged(double value) => Guard(() =>
    {
        SoftLimitText = NumericText.Of(Math.Round(value), "F0");
        KeepTheHardLimitOverTheSoftOne(value);
    });


    /// <summary>
    /// The three of them are one order: the soft limit at or below the hard limit, and the hard limit at or below
    /// the peak power everything is read against. Moving one takes whichever of the others is in its way with it,
    /// which is what the WPF dialog does - refusing the drag would be a worse way to say so.
    /// </summary>
    private void KeepLimitsUnderTheReference(double reference)
    {
        if (HardLimit > reference)
        {
            Write(reference, text => HardLimitText = text, value => HardLimit = value);
        }

        if (SoftLimit > reference)
        {
            Write(reference, text => SoftLimitText = text, value => SoftLimit = value);
        }
    }

    /// <inheritdoc cref="KeepLimitsUnderTheReference"/>
    private void KeepTheSoftLimitUnderTheHardOne(double hardLimit)
    {
        if (SoftLimit > hardLimit)
        {
            Write(hardLimit, text => SoftLimitText = text, value => SoftLimit = value);
        }

        KeepTheReferenceOverTheLimits(hardLimit);
    }

    /// <inheritdoc cref="KeepLimitsUnderTheReference"/>
    private void KeepTheHardLimitOverTheSoftOne(double softLimit)
    {
        if (HardLimit < softLimit)
        {
            Write(softLimit, text => HardLimitText = text, value => HardLimit = value);
        }

        KeepTheReferenceOverTheLimits(Math.Max(softLimit, HardLimit));
    }

    /// <inheritdoc cref="KeepLimitsUnderTheReference"/>
    private void KeepTheReferenceOverTheLimits(double limit)
    {
        if (WattPeakReference < limit)
        {
            Write(limit, text => WattPeakReferenceText = text, value => WattPeakReference = value);
        }
    }

    /// <summary>
    /// One of the three, moved by one of the others rather than by the user. Inside the guard already, so both
    /// halves are written here: the change handlers are not going to run.
    /// </summary>
    private void Write(double value, Action<string?> writeBox, Action<double> moveSlider)
    {
        writeBox(NumericText.Of(Math.Round(value), "F0"));
        moveSlider(Math.Clamp(value, 0d, PowerSliderMaximum));
    }

    #endregion

    #region Watching the settings

    private void AttachTo(Gen24InverterSettings settings)
    {
        settings.PropertyChanged += OnSettingsPropertyChanged;
        settings.PowerLimitSettings.ExportLimits.PropertyChanged += OnSettingsPropertyChanged;
        settings.PowerLimitSettings.ExportLimits.ActivePower.PropertyChanged += OnSettingsPropertyChanged;
        settings.PowerLimitSettings.ExportLimits.ActivePower.HardLimit.PropertyChanged += OnSettingsPropertyChanged;
        settings.PowerLimitSettings.ExportLimits.ActivePower.SoftLimit.PropertyChanged += OnSettingsPropertyChanged;
    }

    private void DetachFrom(Gen24InverterSettings settings)
    {
        settings.PropertyChanged -= OnSettingsPropertyChanged;
        settings.PowerLimitSettings.ExportLimits.PropertyChanged -= OnSettingsPropertyChanged;
        settings.PowerLimitSettings.ExportLimits.ActivePower.PropertyChanged -= OnSettingsPropertyChanged;
        settings.PowerLimitSettings.ExportLimits.ActivePower.HardLimit.PropertyChanged -= OnSettingsPropertyChanged;
        settings.PowerLimitSettings.ExportLimits.ActivePower.SoftLimit.PropertyChanged -= OnSettingsPropertyChanged;
    }

    /// <summary>
    /// The visibility rules read the settings, and a change there is a change of this view model as far as the
    /// view is concerned. Four objects are watched rather than one, because the switches that decide what is
    /// shown are spread over the limits and their two definitions.
    /// </summary>
    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!isLoading)
        {
            ToastText = null;
        }

        switch (e.PropertyName)
        {
            case nameof(Gen24PowerLimit.IsEnabled) when sender is Gen24PowerLimit:
                OnPropertyChanged(nameof(ShowExportLimitDetails));
                break;

            case nameof(Gen24PowerLimitDefinition.IsEnabled) when ReferenceEquals(sender, Limits.HardLimit):
                OnPropertyChanged(nameof(ShowHardLimit));
                break;

            case nameof(Gen24PowerLimitDefinition.IsEnabled) when ReferenceEquals(sender, Limits.SoftLimit):
                OnPropertyChanged(nameof(ShowSoftLimit));
                break;
        }
    }

    private void NotifyAllVisibilities()
    {
        OnPropertyChanged(nameof(ShowExportLimits));
        OnPropertyChanged(nameof(ShowExportLimitDetails));
        OnPropertyChanged(nameof(ShowHardLimit));
        OnPropertyChanged(nameof(ShowSoftLimit));
        OnPropertyChanged(nameof(ShowMppt1));
        OnPropertyChanged(nameof(ShowMppt2));
    }

    #endregion

    /// <summary>The switches that are behind the danger toggle go back to what the inverter holds when it is off.</summary>
    /// <remarks>
    /// The same rule as the energy flow tab: a switch the user opened the guard for and then changed their mind
    /// about should not stay changed just because they closed the guard again.
    /// </remarks>
    partial void OnEnableDangerChanged(bool value)
    {
        if (value || isLoading)
        {
            return;
        }

        Settings.TimeZoneName = loadedSettings.TimeZoneName;
        Settings.TimeSync = loadedSettings.TimeSync;
    }

    private ListItemModel<T> FroniusItem<T>(T value) where T : Enum => new() { Value = value, DisplayName = Channel(FroniusKey(value)) };

    /// <summary>
    /// What the inverter calls one value of an enum. The name of the channel is on the value itself, as the
    /// <c>ParseAs</c> of its <see cref="EnumParseAttribute"/> - the same mapping <c>IGen24Service.GetFroniusName</c>
    /// makes in the WPF app.
    /// </summary>
    private static string FroniusKey<T>(T value) where T : Enum
    {
        var name = value.ToString();

        return typeof(T)
            .GetMember(name)
            .FirstOrDefault()?
            .GetCustomAttribute<EnumParseAttribute>()?
            .ParseAs ?? name;
    }

    private string Config(string key) => gen24Loc.GetLocalizedString(Gen24LocalizationSection.Config, key);

}
