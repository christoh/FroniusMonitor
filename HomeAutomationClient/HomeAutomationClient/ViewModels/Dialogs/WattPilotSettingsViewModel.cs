using System.ComponentModel;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Charging;
using De.Hochstaetter.Fronius.Models.Gen24.Settings;
using De.Hochstaetter.Fronius.Validators;
using Microsoft.AspNetCore.SignalR;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

/// <summary>
/// Every setting of a Wattpilot, ported from <c>WattPilotSettingsViewModel</c> of FroniusMonitor: six tabs of
/// fields over one edited copy of the device, written to the charger in one go.
/// </summary>
/// <remarks>
/// <para>
/// The dialog starts from a copy of the live device the client already holds (<c>loaded</c>) and edits a second
/// copy (<see cref="WattPilot"/>). Apply sends both to the server over the hub - see
/// <see cref="IUpdateService.SetWattPilotSettings"/> - and the server writes whatever differs, one key per changed
/// property, then answers with what the charger refused or never confirmed. The difference is taken against the
/// copy the dialog started from and not against the live device, so a value the charger changed by itself while the
/// dialog was open is not written back to what it was.
/// </para>
/// <para>
/// Every number is a <see cref="NumberField"/>, a box and a slider kept in step with the rule on the text; the
/// flags and the choices bind to the edited copy directly. Every "is this visible" of the WPF window is a bindable
/// property here, re-raised whenever the edited copy changes, as <c>.claude/rules/ViewModelsForInteractionLogic.md</c>
/// asks. The links - the charging log, the configuration PDF, the cloud API - go through
/// <see cref="IUriLauncher"/>, because <c>Process.Start</c> does not exist in the browser.
/// </para>
/// </remarks>
public sealed partial class WattPilotSettingsViewModel : ViewModelBase
{
    private const int MillisecondsPerMinute = 60_000;

    private readonly IUpdateService updateService = IoC.GetRegistered<IUpdateService>();
    private readonly IUriLauncher uriLauncher = IoC.GetRegistered<IUriLauncher>();
    private readonly ITabHost owner;
    private readonly string deviceId;
    private readonly IReadOnlyList<FieldBinding> fields;
    private WattPilot loaded;

    /// <summary>A number of the device on the screen: where it comes from, and when the user can see it.</summary>
    private sealed record FieldBinding(NumberField Field, Func<WattPilot, double?> Read, Func<bool>? IsVisible = null);

    public WattPilotSettingsViewModel(ITabHost owner, string deviceId, WattPilot wattPilot)
    {
        this.owner = owner;
        this.deviceId = deviceId;
        loaded = (WattPilot)wattPilot.Clone();
        WattPilot = (WattPilot)loaded.Clone();
        AttachTo(WattPilot);

        fields =
        [
            new(MaximumChargingCurrent, w => w.MaximumChargingCurrent),
            new(MinimumSocInBoost, w => w.MinimumSocInBoost, () => ShowBoost),
            new(MaxEnergyPrice, w => w.MaxEnergyPrice, () => ShowAwattar),
            new(PvSurplusBatteryLevelStartCharge, w => w.PvSurplusBatteryLevelStartCharge, () => ShowPvSurplus),
            new(PvSurplusBatteryLevelStopCharge, w => w.PvSurplusBatteryLevelStopCharge, () => ShowBatteryOptions),
            new(OhmPilotTemperatureLimit, w => w.OhmPilotTemperatureLimitCelsius, () => ShowPvSurplus),
            new(PvSurplusPowerThreshold, w => w.PvSurplusPowerThreshold, () => ShowPvSurplus),
            new(NextTripEnergyToCharge, w => w.NextTripEnergyToCharge / 1000d, () => ShowPvSurplus),
            new(DynamicMaximumCurrent, w => w.LoadBalancingCurrents?.DynamicMaximumCurrent, () => ShowLoadBalancing),
            new(MaximumCurrentDnoLine, w => w.LoadBalancingCurrents?.MaximumCurrentDnoLine, () => ShowLoadBalancing),
            new(LoadBalancingFallbackCurrent, w => w.LoadBalancingFallbackCurrent, () => ShowLoadBalancing),
            new(MinimumChargingCurrent, w => w.MinimumChargingCurrent),
            new(MinimumChargingInterval, w => w.MinimumChargingInterval is > 0 and var interval ? interval / (double)MillisecondsPerMinute : null, () => ShowChargingInterval),
            new(MinimumPauseDuration, w => w.MinimumPauseDuration / (double?)MillisecondsPerMinute, () => ShowChargingPause),
            new(MinimumChargingTime, w => w.MinimumChargingTime / (double?)MillisecondsPerMinute, () => ShowChargingPause),
            new(PhaseSwitchPower, w => w.PhaseSwitchPower, () => ShowPhaseSwitch),
            new(PhaseSwitchTriggerTime, w => w.PhaseSwitchTriggerTime / (double?)MillisecondsPerMinute, () => ShowPhaseSwitch),
            new(MinimumTimeBetweenPhaseSwitches, w => w.MinimumTimeBetweenPhaseSwitches / (double?)MillisecondsPerMinute, () => ShowPhaseSwitch),
            new(MaximumOutOfBalanceCurrent, w => w.MaximumOutOfBalanceCurrent, () => ShowOutOfBalance),
            new(AbsoluteMaximumChargingCurrent, w => w.AbsoluteMaximumChargingCurrent),
            new(MaximumChargingCurrentPhase1, w => w.MaximumChargingCurrentPhase1),
            new(RandomDelayPowerFailure, w => w.RandomDelayPowerFailure),
            new(RandomDelayAwattarStart, w => w.RandomDelayAwattarStart),
            new(RandomDelayAwattarStop, w => w.RandomDelayAwattarStop),
            new(RandomDelayTimerStart, w => w.RandomDelayTimerStart),
            new(RandomDelayTimerStop, w => w.RandomDelayTimerStop),
            new(RandomDelayCarConnect, w => w.RandomDelayCarConnect),
            new(GridMonitoringTimeOnStartUp, w => w.GridMonitoringTimeOnStartUp, () => ShowGridMonitoring),
            new(StartUpMonitoringMinimumVoltage, w => w.StartUpMonitoringMinimumVoltage, () => ShowGridMonitoring),
            new(StartUpMonitoringMaximumVoltage, w => w.StartUpMonitoringMaximumVoltage, () => ShowGridMonitoring),
            new(StartUpMonitoringMinimumFrequency, w => w.StartUpMonitoringMinimumFrequency, () => ShowGridMonitoring),
            new(StartUpMonitoringMaximumFrequency, w => w.StartUpMonitoringMaximumFrequency, () => ShowGridMonitoring),
            new(StartUpRampUpRate, w => w.StartUpRampUpRate, () => ShowGridMonitoring),
        ];

        // The ranges of the current sliders follow what the DNO tab allows, live, the way the WPF sliders were bound
        // to the device's own limits.
        AbsoluteMaximumChargingCurrent.PropertyChanged += OnLimitChanged;
        MinimumChargingCurrent.PropertyChanged += OnLimitChanged;

        CopyFromWattPilot();
    }

    #region Proxies to the dialog

    /// <summary>A tab has no busy indicator of its own; the dialog around it has one. See <see cref="ITabHost"/>.</summary>
    public override string? BusyText
    {
        get => owner.BusyText;
        set => owner.BusyText = value;
    }

    public string? ToastText
    {
        get => owner.ToastText;
        set => owner.ToastText = value;
    }

    #endregion

    #region The edited device and the choices offered

    [ObservableProperty]
    public partial WattPilot WattPilot { get; set; }

    /// <summary>The settings that can take a charger off the grid or its network stay disabled until the user says they mean it.</summary>
    [ObservableProperty]
    public partial bool EnableDanger { get; set; }

    public IReadOnlyList<EnumListItemModel<CableLockBehavior>> CableLockBehaviors { get; } = Items<CableLockBehavior>();
    public IReadOnlyList<EnumListItemModel<ChargingLogic>> ChargingLogics { get; } = Items<ChargingLogic>();
    public IReadOnlyList<EnumListItemModel<ForcedCharge>> ForcedCharges { get; } = Items<ForcedCharge>(sortByName: true);
    public IReadOnlyList<EnumListItemModel<AwattarCountry>> EnergyPriceCountries { get; } = Items<AwattarCountry>(sortByName: true);
    public IReadOnlyList<EnumListItemModel<EcoRoundingMode>> EcoRoundingModes { get; } = Items<EcoRoundingMode>();
    public IReadOnlyList<EnumListItemModel<PhaseSwitchMode>> PhaseSwitchModes { get; } = Items<PhaseSwitchMode>();
    public IReadOnlyList<EnumListItemModel<LoadBalancingPriority>> LoadBalancingPriorities { get; } = Items<LoadBalancingPriority>();
    public IReadOnlyList<WattPilotPhaseMap> PhaseMaps => WattPilotPhaseMap.All;

    #endregion

    #region The numbers, as box and slider pairs

    public NumberField MaximumChargingCurrent { get; } = Integer(nameof(Loc.MaximumChargingCurrent), 0, 32, sliderMinimum: 6);
    public NumberField MinimumSocInBoost { get; } = Integer(nameof(Loc.MinimumSocInBoost), 0, 100);

    public NumberField MaxEnergyPrice { get; } = Number(nameof(Loc.MaxEnergyPrice), -200, 1000, sliderMinimum: -10, sliderMaximum: 100, tickFrequency: 0.1, decimals: 1);
    public NumberField PvSurplusBatteryLevelStartCharge { get; } = Integer(nameof(Loc.PvSurplusBatteryLevel), 0, 100);
    public NumberField PvSurplusBatteryLevelStopCharge { get; } = Integer(nameof(Loc.PvSurplusBatteryLevelStopCharge), 0, 100);
    public NumberField OhmPilotTemperatureLimit { get; } = Integer(nameof(Loc.OhmPilotTemperatureLimit), 0, 100);
    public NumberField PvSurplusPowerThreshold { get; } = Integer(nameof(Loc.PvSurplusPowerThreshold), 0, 22000);
    public NumberField NextTripEnergyToCharge { get; } = Number(nameof(Loc.NextTripEnergyToCharge), 0, 300, sliderMaximum: 150, decimals: 1);

    public NumberField DynamicMaximumCurrent { get; } = Integer(nameof(Loc.LoadBalancingSharedMaxCurrent), 6, 96);
    public NumberField MaximumCurrentDnoLine { get; } = Integer(nameof(Loc.MaximumCurrentDnoLine), 6, 100);

    /// <summary>6 to 32 A or 0, the one rule with a hole in it - see <see cref="WattPilotFallbackCurrentAttribute"/>.</summary>
    public NumberField LoadBalancingFallbackCurrent { get; } =
        new(new WattPilotFallbackCurrentAttribute { PropertyDisplayNameResourceKey = nameof(Loc.MaximumFallbackCurrent) }, 0, 32);

    public NumberField MinimumChargingCurrent { get; } = Integer(nameof(Loc.MinimumChargingCurrent), 6, 32);
    public NumberField MinimumChargingInterval { get; } = Integer(nameof(Loc.MinimumChargingInterval), 5, 35791, sliderMaximum: 240);
    public NumberField MinimumPauseDuration { get; } = Integer(nameof(Loc.MinimumPauseDuration), 0, 35791, sliderMaximum: 20);
    public NumberField MinimumChargingTime { get; } = Integer(nameof(Loc.MinimumChargingTime), 0, 35791, sliderMaximum: 20);
    public NumberField PhaseSwitchPower { get; } = Integer(nameof(Loc.PhaseSwitchPower), 1400, 7200);
    public NumberField PhaseSwitchTriggerTime { get; } = Integer(nameof(Loc.PhaseSwitchTriggerTime), 0, 35791, sliderMaximum: 20);
    public NumberField MinimumTimeBetweenPhaseSwitches { get; } = Integer(nameof(Loc.MinimumTimeBetweenPhaseSwitches), 0, 35791, sliderMaximum: 20);

    public NumberField MaximumOutOfBalanceCurrent { get; } = Integer(nameof(Loc.MaximumOutOfBalanceCurrent), 0, 64, sliderMinimum: 6, sliderMaximum: 32);
    public NumberField AbsoluteMaximumChargingCurrent { get; } = Integer(nameof(Loc.AbsoluteMaximumChargingCurrent), 0, 32, sliderMinimum: 6);
    public NumberField MaximumChargingCurrentPhase1 { get; } = Integer(nameof(Loc.MaximumChargingCurrentPhase1), 0, 16, sliderMinimum: 6);
    public NumberField RandomDelayPowerFailure { get; } = Integer(nameof(Loc.RandomDelayPowerFailure), 0, 1200, tickFrequency: 20);
    public NumberField RandomDelayAwattarStart { get; } = Integer(nameof(Loc.RandomDelayAwattarStart), 0, 1200, tickFrequency: 20);
    public NumberField RandomDelayAwattarStop { get; } = Integer(nameof(Loc.RandomDelayAwattarStop), 0, 1200, tickFrequency: 20);
    public NumberField RandomDelayTimerStart { get; } = Integer(nameof(Loc.RandomDelayTimerStart), 0, 1200, tickFrequency: 20);
    public NumberField RandomDelayTimerStop { get; } = Integer(nameof(Loc.RandomDelayTimerStop), 0, 1200, tickFrequency: 20);
    public NumberField RandomDelayCarConnect { get; } = Integer(nameof(Loc.RandomDelayCarConnect), 0, 1200, tickFrequency: 20);
    public NumberField GridMonitoringTimeOnStartUp { get; } = Number(nameof(Loc.GridMonitoringTimeOnStartUp), 0, 900);
    public NumberField StartUpMonitoringMinimumVoltage { get; } = Number(nameof(Loc.StartUpMonitoringMinimumVoltage), 107, 311, sliderMinimum: 150);
    public NumberField StartUpMonitoringMaximumVoltage { get; } = Number(nameof(Loc.StartUpMonitoringMaximumVoltage), 107, 311, sliderMinimum: 150);
    public NumberField StartUpMonitoringMinimumFrequency { get; } = Number(nameof(Loc.StartUpMonitoringMinimumFrequency), 45, 66, tickFrequency: 0.1, decimals: 1);
    public NumberField StartUpMonitoringMaximumFrequency { get; } = Number(nameof(Loc.StartUpMonitoringMaximumFrequency), 45, 66, tickFrequency: 0.1, decimals: 1);
    public NumberField StartUpRampUpRate { get; } = Number(nameof(Loc.StartUpRampUpRate), 0.001, 100, tickFrequency: 0.1, decimals: 3);

    #endregion

    #region Text the device keeps as something else

    /// <summary>The departure time, <c>HH:mm</c>. The device keeps seconds from midnight (<c>ftt</c>).</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [TimeOfDay(PropertyDisplayNameResourceKey = nameof(Loc.NextTripTime))]
    public partial string? NextTripTimeText { get; set; }

    /// <summary>From when the battery may be used, <c>HH:mm</c>. The device keeps seconds (<c>pdls</c>).</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [TimeOfDay(PropertyDisplayNameResourceKey = nameof(Loc.FromBatteryOnlyFrom))]
    public partial string? BatteryFromText { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [TimeOfDay(PropertyDisplayNameResourceKey = nameof(Loc.To))]
    public partial string? BatteryToText { get; set; }

    /// <summary>
    /// The password of the WiFi the charger joins. Write-only on the charger - it never reports it - so the box
    /// starts empty and only what the user actually types is sent.
    /// </summary>
    [ObservableProperty]
    public partial string WifiPassword { get; set; } = string.Empty;

    #endregion

    #region Switches that stand for something the device says differently

    /// <summary>Whether to insist on a minimum charging interval at all; off is 0 on the charger (<c>mci</c>).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowChargingInterval))]
    public partial bool RequiresChargingInterval { get; set; }

    /// <summary>Authenticated as the guest, or nobody. <c>trx</c>: null is nobody, 0 is the guest, n is card n-1.</summary>
    [ObservableProperty]
    public partial bool IsUserAuthenticated { get; set; }

    /// <summary><see cref="AccessMode.RequireAuth"/> against <see cref="AccessMode.Everyone"/>.</summary>
    [ObservableProperty]
    public partial bool RequireRfidAuth { get; set; }

    /// <summary>The inverse of <c>ebo</c>: a boost until the car is unplugged is not a single boost.</summary>
    [ObservableProperty]
    public partial bool BoostUntilCarDisconnects { get; set; }

    #endregion

    #region What is on screen

    public bool ShowBoost => WattPilot.EnableBatteryBoost is true;
    public bool ShowPvSurplus => WattPilot.PvSurplusEnabled is true;
    public bool ShowBatteryOptions => WattPilot.CanModifyChargingFromBatteryOptions;
    public bool ShowAwattar => WattPilot.AwattarEnabled is true;
    public bool ShowLoadBalancing => WattPilot.LoadBalancingEnabled is true;
    public bool ShowChargingPause => WattPilot.AllowChargingPause is true;
    public bool ShowPhaseSwitch => WattPilot.AllowPauseAndHasPhaseSwitch is true;
    public bool ShowOutOfBalance => WattPilot.EnableOutOfBalanceControl is true;
    public bool ShowGridMonitoring => WattPilot.EnableGridMonitoringOnStartUp is true;
    public bool ShowChargingInterval => RequiresChargingInterval;
    public bool HasDownloadLink => !string.IsNullOrWhiteSpace(WattPilot.DownloadLink);

    public string ApiLink => $"https://{WattPilot.SerialNumber ?? "<Serial>"}.api.v3.go-e.io";
    public string ApiUri => $"{ApiLink}/api/status?token={WattPilot.CloudAccessKey ?? "<Token>"}";

    #endregion

    #region Commands

    /// <summary>Unconditionally back to what the dialog started from, or to what the last Apply wrote.</summary>
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
        RestoreRefusedFieldsThatAreHidden();

        IReadOnlyList<string> errors =
        [
            .. fields.Where(f => f.Field.HasErrors).Select(f => f.Field.Error!)
                .Concat(GetErrors().Select(error => error.ErrorMessage).Where(message => !string.IsNullOrWhiteSpace(message)).Select(message => message!))
                .Distinct(),
        ];

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

        CopyToWattPilot();

        // Nothing to send, so nothing is sent - what the WPF dialog does. The server takes the same difference
        // again; this only answers whether the user changed anything since the dialog opened.
        if (WattPilot.ChangedSettings(loaded).Count == 0)
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

        BusyText = string.Format(CultureInfo.CurrentCulture, Loc.SavingSettings, Loc.WattPilotHeader);
        WattPilotWriteResult result;

        try
        {
            result = await updateService.SetWattPilotSettings(deviceId, WattPilot, loaded).ConfigureAwait(true);
        }
        catch (HubException ex)
        {
            BusyText = null;
            await ex.ShowHubError().ConfigureAwait(true);
            return;
        }

        BusyText = null;

        // What was sent is what Undo goes back to from now on, whatever the charger made of it: the ones it took are
        // there, and the ones it refused are what the message box says.
        loaded = (WattPilot)WattPilot.Clone();
        Reset();

        if (result.IsSuccess)
        {
            ToastText = Loc.SentToWattPilot;
            return;
        }

        if (result.Errors.Count > 0)
        {
            await new MessageBox { Text = Loc.SettingsNotWritten, ItemList = result.Errors, Title = Loc.Error, Buttons = [Loc.Ok], Icon = new ErrorIcon() }.Show().ConfigureAwait(true);
        }

        if (result.Unconfirmed.Count > 0)
        {
            await new MessageBox { Text = Loc.SettingsNotConfirmed, ItemList = result.Unconfirmed, Title = Loc.Warning, Buttons = [Loc.Ok], Icon = new WarningIcon() }.Show().ConfigureAwait(true);
        }
    });

    [RelayCommand]
    private Task Reboot() => TaskExceptionHandler(async () =>
    {
        var answer = await new MessageBox
        {
            Text = Loc.ConfirmReboot,
            Title = Loc.Reboot,
            Buttons = [Loc.Reboot, Loc.Cancel],
            Icon = new WarningIcon(),
        }.Show().ConfigureAwait(true);

        if (answer?.Index != 0)
        {
            return;
        }

        BusyText = Loc.Reboot;

        try
        {
            await updateService.RebootWattPilot(deviceId).ConfigureAwait(true);
        }
        catch (HubException ex)
        {
            BusyText = null;
            await ex.ShowHubError().ConfigureAwait(true);
        }
    });

    [RelayCommand]
    private Task OpenChargingLog() => Launch(WattPilot.DownloadLink);

    /// <summary>The charger's own documentation of its configuration, in the language of the app.</summary>
    [RelayCommand]
    private Task OpenConfigPdf() => Launch(WattPilot.DownloadLink is { } link
        ? $"{link.Replace("export", "documentation")}&lang={CultureInfo.CurrentUICulture.TwoLetterISOLanguageName}"
        : null);

    [RelayCommand]
    private Task NavigateToApi() => Launch(ApiUri);

    private Task Launch(string? link) => TaskExceptionHandler(async () =>
    {
        if (string.IsNullOrWhiteSpace(link))
        {
            throw new InvalidOperationException(Loc.NoChargingLogFromWattPilot);
        }

        await uriLauncher.LaunchAsync(new Uri(link)).ConfigureAwait(true);
    });

    #endregion

    #region Between the device and the screen

    private void Reset()
    {
        DetachFrom(WattPilot);
        WattPilot = (WattPilot)loaded.Clone();
        AttachTo(WattPilot);
        CopyFromWattPilot();
    }

    /// <summary>The device into every field, switch and text, then the ranges that follow other fields.</summary>
    private void CopyFromWattPilot()
    {
        foreach (var binding in fields)
        {
            binding.Field.Load(binding.Read(WattPilot));
        }

        NextTripTimeText = WattPilot.NextTripTime is { } seconds ? TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm", CultureInfo.InvariantCulture) : null;
        BatteryFromText = WattPilot.AllowChargingFromBatteryStartSeconds is null ? null : WattPilot.AllowChargingFromBatteryStartString;
        BatteryToText = WattPilot.AllowChargingFromBatteryStopSeconds is null ? null : WattPilot.AllowChargingFromBatteryStopString;
        WifiPassword = string.Empty;

        RequiresChargingInterval = (WattPilot.MinimumChargingInterval ?? 0) != 0;
        IsUserAuthenticated = WattPilot.AuthenticatedCardIndex.HasValue;
        RequireRfidAuth = WattPilot.AccessMode == AccessMode.RequireAuth;
        BoostUntilCarDisconnects = WattPilot.EnableSingleTimeBoost is false;

        // A charger that may not pause cannot switch phases either, so it is on three - the rule the WPF dialog
        // applies on load and again on Apply, so the two never disagree on the wire.
        if (WattPilot.AllowChargingPause is false)
        {
            WattPilot.PhaseSwitchMode = PhaseSwitchMode.Phases3;
        }

        FollowLimits();
        NotifyAllVisibilities();

        // Explicitly: a setter validates only what it actually stores, so writing the same value twice - at load,
        // or after Undo - would leave whatever error was there standing.
        ValidateAllProperties();
    }

    /// <summary>And back again, on the way to the charger. Only ever called once the rules have passed.</summary>
    private void CopyToWattPilot()
    {
        WattPilot.MaximumChargingCurrent = (byte?)MaximumChargingCurrent.IntegerValue;
        WattPilot.MinimumSocInBoost = (byte?)MinimumSocInBoost.IntegerValue;
        WattPilot.MaxEnergyPrice = (float?)MaxEnergyPrice.Value;
        WattPilot.PvSurplusBatteryLevelStartCharge = (int?)PvSurplusBatteryLevelStartCharge.IntegerValue;
        WattPilot.PvSurplusBatteryLevelStopCharge = (byte?)PvSurplusBatteryLevelStopCharge.IntegerValue;
        WattPilot.OhmPilotTemperatureLimitCelsius = (int?)OhmPilotTemperatureLimit.IntegerValue;
        WattPilot.PvSurplusPowerThreshold = (float?)PvSurplusPowerThreshold.Value;
        WattPilot.NextTripEnergyToCharge = NextTripEnergyToCharge.Value is { } kiloWattHours ? (int)Math.Round(kiloWattHours * 1000, MidpointRounding.AwayFromZero) : null;
        WattPilot.LoadBalancingFallbackCurrent = (int?)LoadBalancingFallbackCurrent.IntegerValue;
        WattPilot.MinimumChargingCurrent = (byte?)MinimumChargingCurrent.IntegerValue;
        WattPilot.MinimumChargingInterval = RequiresChargingInterval ? Milliseconds(MinimumChargingInterval) : 0;
        WattPilot.MinimumPauseDuration = Milliseconds(MinimumPauseDuration);
        WattPilot.MinimumChargingTime = Milliseconds(MinimumChargingTime);
        WattPilot.PhaseSwitchPower = (float?)PhaseSwitchPower.Value;
        WattPilot.PhaseSwitchTriggerTime = Milliseconds(PhaseSwitchTriggerTime);
        WattPilot.MinimumTimeBetweenPhaseSwitches = Milliseconds(MinimumTimeBetweenPhaseSwitches);
        WattPilot.MaximumOutOfBalanceCurrent = (byte?)MaximumOutOfBalanceCurrent.IntegerValue;
        WattPilot.AbsoluteMaximumChargingCurrent = (byte?)AbsoluteMaximumChargingCurrent.IntegerValue;
        WattPilot.MaximumChargingCurrentPhase1 = (byte?)MaximumChargingCurrentPhase1.IntegerValue;
        WattPilot.RandomDelayPowerFailure = (int?)RandomDelayPowerFailure.IntegerValue;
        WattPilot.RandomDelayAwattarStart = (int?)RandomDelayAwattarStart.IntegerValue;
        WattPilot.RandomDelayAwattarStop = (int?)RandomDelayAwattarStop.IntegerValue;
        WattPilot.RandomDelayTimerStart = (int?)RandomDelayTimerStart.IntegerValue;
        WattPilot.RandomDelayTimerStop = (int?)RandomDelayTimerStop.IntegerValue;
        WattPilot.RandomDelayCarConnect = (int?)RandomDelayCarConnect.IntegerValue;
        WattPilot.GridMonitoringTimeOnStartUp = (int)(GridMonitoringTimeOnStartUp.IntegerValue ?? WattPilot.GridMonitoringTimeOnStartUp);
        WattPilot.StartUpMonitoringMinimumVoltage = (float?)StartUpMonitoringMinimumVoltage.Value;
        WattPilot.StartUpMonitoringMaximumVoltage = (float?)StartUpMonitoringMaximumVoltage.Value;
        WattPilot.StartUpMonitoringMinimumFrequency = (float?)StartUpMonitoringMinimumFrequency.Value;
        WattPilot.StartUpMonitoringMaximumFrequency = (float?)StartUpMonitoringMaximumFrequency.Value;
        WattPilot.StartUpRampUpRate = (float?)StartUpRampUpRate.Value;

        // The load balancing currents go to the charger as one object, and the charger wants a fresh timestamp
        // with every change of it. Compared with what the dialog started from, so an unchanged object is not sent.
        if (WattPilot.LoadBalancingCurrents is { } currents)
        {
            currents.DynamicMaximumCurrent = (int)(DynamicMaximumCurrent.IntegerValue ?? currents.DynamicMaximumCurrent);
            currents.MaximumCurrentDnoLine = (int)(MaximumCurrentDnoLine.IntegerValue ?? currents.MaximumCurrentDnoLine);

            if (currents != loaded.LoadBalancingCurrents)
            {
                currents.TimeStamp = DateTime.UtcNow;
            }
        }

        if (Gen24ChargingRule.GetDate(NextTripTimeText) is { } departure)
        {
            WattPilot.NextTripTime = Math.Min(departure.Hour * 3600 + departure.Minute * 60, 86399);
        }

        if (!string.IsNullOrWhiteSpace(BatteryFromText))
        {
            WattPilot.AllowChargingFromBatteryStartString = BatteryFromText;
        }

        if (!string.IsNullOrWhiteSpace(BatteryToText))
        {
            WattPilot.AllowChargingFromBatteryStopString = BatteryToText;
        }

        if (!string.IsNullOrWhiteSpace(WifiPassword))
        {
            WattPilot.WifiPassword = WifiPassword;
        }

        WattPilot.AuthenticatedCardIndex = IsUserAuthenticated ? WattPilot.AuthenticatedCardIndex ?? 0 : null;
        WattPilot.AccessMode = RequireRfidAuth ? AccessMode.RequireAuth : AccessMode.Everyone;
        WattPilot.EnableSingleTimeBoost = !BoostUntilCarDisconnects;

        if (WattPilot.AllowChargingPause is false)
        {
            WattPilot.PhaseSwitchMode = PhaseSwitchMode.Phases3;
        }
    }

    /// <summary>
    /// A field the user cannot see must not stop them saving: anything both refused and off screen goes back to what
    /// the dialog started from. The WPF dialog did the same by walking its visual tree for invisible boxes in error.
    /// </summary>
    private void RestoreRefusedFieldsThatAreHidden()
    {
        foreach (var binding in fields.Where(f => f.IsVisible?.Invoke() == false && f.Field.HasErrors))
        {
            binding.Field.Load(binding.Read(loaded));
        }

        if (ShowBatteryOptions)
        {
            return;
        }

        if (GetErrors(nameof(BatteryFromText)).Any())
        {
            BatteryFromText = loaded.AllowChargingFromBatteryStartSeconds is null ? null : loaded.AllowChargingFromBatteryStartString;
        }

        if (GetErrors(nameof(BatteryToText)).Any())
        {
            BatteryToText = loaded.AllowChargingFromBatteryStopSeconds is null ? null : loaded.AllowChargingFromBatteryStopString;
        }
    }

    /// <summary>The current sliders end where the absolute maximum allows, and the maximum current starts at the minimum.</summary>
    private void FollowLimits()
    {
        var chargerLimit = WattPilot.MaxWattPilotCurrent ?? 32;
        var absoluteMaximum = AbsoluteMaximumChargingCurrent.Value ?? chargerLimit;

        AbsoluteMaximumChargingCurrent.Maximum = chargerLimit;
        MaximumOutOfBalanceCurrent.Maximum = chargerLimit;
        MaximumChargingCurrent.Maximum = absoluteMaximum;
        MinimumChargingCurrent.Maximum = absoluteMaximum;
        MaximumChargingCurrent.Minimum = MinimumChargingCurrent.Value ?? 6;
        PvSurplusPowerThreshold.Maximum = WattPilot.MaximumWattPilotPower ?? 22000;
    }

    private void OnLimitChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NumberField.Text))
        {
            FollowLimits();
        }
    }

    private static int? Milliseconds(NumberField minutes) => minutes.IntegerValue is { } value ? (int)(value * MillisecondsPerMinute) : null;

    partial void OnRequiresChargingIntervalChanged(bool value)
    {
        // Five minutes is the least the charger takes; switching the interval on lands there rather than on 0.
        if (value && (MinimumChargingInterval.Value ?? 0) < 5)
        {
            MinimumChargingInterval.Load(5);
        }
    }

    private void AttachTo(WattPilot wattPilot) => wattPilot.PropertyChanged += OnWattPilotPropertyChanged;

    private void DetachFrom(WattPilot wattPilot) => wattPilot.PropertyChanged -= OnWattPilotPropertyChanged;

    /// <summary>
    /// The visibility rules read the edited copy, and a change there is a change of this view model as far as the
    /// view is concerned. All of them are re-raised rather than the ones that could have changed: they are cheap,
    /// and the list of which flag drives which group is exactly the kind of table that goes stale.
    /// </summary>
    private void OnWattPilotPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        ToastText = null;
        NotifyAllVisibilities();

        if (e.PropertyName is nameof(WattPilot.Is16AmpereVariant) or nameof(WattPilot.MaximumWattPilotPowerKiloWatts))
        {
            FollowLimits();
        }
    }

    private void NotifyAllVisibilities()
    {
        NotifyOfPropertyChange(nameof(ShowBoost));
        NotifyOfPropertyChange(nameof(ShowPvSurplus));
        NotifyOfPropertyChange(nameof(ShowBatteryOptions));
        NotifyOfPropertyChange(nameof(ShowAwattar));
        NotifyOfPropertyChange(nameof(ShowLoadBalancing));
        NotifyOfPropertyChange(nameof(ShowChargingPause));
        NotifyOfPropertyChange(nameof(ShowPhaseSwitch));
        NotifyOfPropertyChange(nameof(ShowOutOfBalance));
        NotifyOfPropertyChange(nameof(ShowGridMonitoring));
        NotifyOfPropertyChange(nameof(HasDownloadLink));
        NotifyOfPropertyChange(nameof(ApiLink));
        NotifyOfPropertyChange(nameof(ApiUri));
    }

    private static NumberField Integer(string displayNameKey, long minimum, long maximum, double? sliderMinimum = null, double? sliderMaximum = null, double tickFrequency = 1) =>
        new(new MinMaxIntAttribute(minimum, maximum) { PropertyDisplayNameResourceKey = displayNameKey }, sliderMinimum ?? minimum, sliderMaximum ?? maximum, tickFrequency);

    private static NumberField Number(string displayNameKey, double minimum, double maximum, double? sliderMinimum = null, double? sliderMaximum = null, double tickFrequency = 1, int decimals = 0) =>
        new(new MinMaxDoubleAttribute(minimum, maximum) { PropertyDisplayNameResourceKey = displayNameKey }, sliderMinimum ?? minimum, sliderMaximum ?? maximum, tickFrequency, decimals);

    private static EnumListItemModel<T>[] Items<T>(bool sortByName = false) where T : struct, Enum
    {
        var items = Enum.GetValues<T>().Select(value => new EnumListItemModel<T> { Value = value });
        return [.. sortByName ? items.OrderBy(item => item.DisplayName, StringComparer.CurrentCulture) : items];
    }

    #endregion
}
