using System.Collections.ObjectModel;
using De.Hochstaetter.Fronius.Models.ToshibaAc;
using Microsoft.AspNetCore.SignalR;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

/// <summary>
/// The interaction logic of one Toshiba air conditioner on the dashboard: what each button does, which choices the
/// menus offer for this model, and how a command travels and is confirmed. <c>ToshibaHvacControl</c> resolves one
/// of these per device and hands it the device and its key; everything the WPF control did in its code behind
/// lives here.
/// </summary>
/// <remarks>
/// <para>
/// A command is a delta: a fresh <see cref="ToshibaHvacStateData"/> is all 0xff, and only the bytes set here go to
/// the air conditioner. It is sent through the server (<see cref="IToshibaHvacCommander"/>), which waits for the
/// echo; while that is pending <see cref="IsSending"/> is true and every command is disabled, as the WPF control
/// disabled itself for up to ten seconds. A target that did not echo is reported to the user - the command may have
/// been carried out all the same, the display shows what the device last said.
/// </para>
/// <para>
/// The state the view shows is the device's own, not a copy: the texts below read <see cref="Device"/>.State
/// and are re-announced whenever it changes, and the menu options' <see cref="HvacOption.IsSelected"/> are
/// written from it. The server's update handler copies new values into the same instance, so the subscription
/// made here stays valid.
/// </para>
/// </remarks>
public partial class ToshibaHvacViewModel(IToshibaHvacCommander commander) : ViewModelBase
{
    public const sbyte MinimumTemperature = 17;
    public const sbyte MaximumTemperature = 30;
    private const sbyte DefaultTemperature = 22;

    /// <summary>What a click on the fan button steps through, in this order - the WPF control's order.</summary>
    private static readonly IReadOnlyList<ToshibaHvacFanSpeed> fanSpeedCycle =
    [
        ToshibaHvacFanSpeed.Manual1, ToshibaHvacFanSpeed.Manual2, ToshibaHvacFanSpeed.Manual3,
        ToshibaHvacFanSpeed.Manual4, ToshibaHvacFanSpeed.Manual5,
        ToshibaHvacFanSpeed.Auto, ToshibaHvacFanSpeed.Quiet,
    ];

    private static readonly IReadOnlyList<byte> powerLimitCycle = [50, 75, 100];

    private IReadOnlyList<ToshibaHvacMeritFeaturesA> meritFeatureCycle = [];
    private IReadOnlyList<ToshibaHvacSwingMode> swingModeCycle = [];

    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanSend))]
    public partial ToshibaHvacMappingDevice? Device { get; set; }

    /// <summary>The id the server publishes the device under - what a command names it by.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanSend))]
    public partial string? DeviceKey { get; set; }

    /// <summary>A command is on its way and its echo is awaited; the buttons are disabled meanwhile.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanSend))]
    public partial bool IsSending { get; private set; }

    public bool CanSend => !IsSending && Device != null && DeviceKey != null;

    public ObservableCollection<FanSpeedOption> FanSpeeds { get; } = [];
    public ObservableCollection<PowerLimitOption> PowerLimits { get; } = [];
    public ObservableCollection<MeritFeatureOption> MeritFeatures { get; } = [];
    public ObservableCollection<SwingModeOption> SwingModes { get; } = [];
    public ObservableCollection<TemperatureOption> Temperatures { get; } = [];

    /// <summary>
    /// The set temperature as the device means it: in the 8 °C heating mode the byte carries the temperature plus
    /// 16, so 24 reads as 8 °C, which is how Toshiba's own app shows it.
    /// </summary>
    public sbyte? DisplayTargetTemperature => Device?.State.TargetTemperatureCelsius is { } target
        ? Device.State.MeritFeaturesA == ToshibaHvacMeritFeaturesA.Heating8C ? (sbyte)(target - 16) : target
        : null;

    public string TargetTemperatureText => Format(DisplayTargetTemperature);
    public string IndoorTemperatureText => Format(Device?.State.CurrentIndoorTemperatureCelsius);
    public string OutdoorTemperatureText => Format(Device?.State.CurrentOutdoorTemperatureCelsius);

    private static string Format(sbyte? temperature) => temperature is { } t ? $"{t:00}°C" : "-- °C";

    /// <summary>The merit features this model offers, from the WPF control: model 2 and 3 have Eco and Hi Power, the rest is in the feature bits.</summary>
    public static IReadOnlyList<ToshibaHvacMeritFeaturesA> AvailableMeritFeatures(ToshibaHvacMappingDevice device)
    {
        var result = new List<ToshibaHvacMeritFeaturesA>(10) { ToshibaHvacMeritFeaturesA.None };

        if (device.AcModelId is 2 or 3)
        {
            result.AddRange([ToshibaHvacMeritFeaturesA.Eco, ToshibaHvacMeritFeaturesA.HighPower]);

            if ((device.MeritFeature & 0x2000) != 0)
            {
                result.AddRange([ToshibaHvacMeritFeaturesA.Silent2, ToshibaHvacMeritFeaturesA.Silent1]);
            }

            if ((device.MeritFeature & 0x0400) != 0)
            {
                result.Add(ToshibaHvacMeritFeaturesA.Heating8C);
            }

            if ((device.MeritFeature & 0x8000) != 0)
            {
                result.Add(ToshibaHvacMeritFeaturesA.Floor);
            }
        }

        return result;
    }

    /// <summary>The swing modes this model offers: horizontal swing is a feature bit, the fixed louver positions exist on model 3 with bit 1.</summary>
    public static IReadOnlyList<ToshibaHvacSwingMode> AvailableSwingModes(ToshibaHvacMappingDevice device)
    {
        var result = new List<ToshibaHvacSwingMode>(10) { ToshibaHvacSwingMode.Off, ToshibaHvacSwingMode.Vertical };

        if ((device.MeritFeature & 0x4000) != 0)
        {
            result.AddRange([ToshibaHvacSwingMode.Horizontal, ToshibaHvacSwingMode.Both]);
        }

        if (device.AcModelId == 3 && (device.MeritFeature & 0x2) != 0)
        {
            result.AddRange([ToshibaHvacSwingMode.Fixed1, ToshibaHvacSwingMode.Fixed2, ToshibaHvacSwingMode.Fixed3, ToshibaHvacSwingMode.Fixed4, ToshibaHvacSwingMode.Fixed5]);
        }

        return result;
    }

    partial void OnDeviceChanged(ToshibaHvacMappingDevice? oldValue, ToshibaHvacMappingDevice? newValue)
    {
        if (oldValue != null)
        {
            oldValue.PropertyChanged -= OnDevicePropertyChanged;
            oldValue.State.PropertyChanged -= OnStatePropertyChanged;
        }

        if (newValue != null)
        {
            newValue.PropertyChanged += OnDevicePropertyChanged;
            newValue.State.PropertyChanged += OnStatePropertyChanged;
        }

        RebuildOptions();
        NotifyCanSendChanged();
    }

    partial void OnDeviceKeyChanged(string? value) => NotifyCanSendChanged();

    partial void OnIsSendingChanged(bool value) => NotifyCanSendChanged();

    private void OnDevicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ToshibaHvacMappingDevice.AcModelId):
            case nameof(ToshibaHvacMappingDevice.MeritFeature):
            case "":
            case null:
                RebuildOptions();
                break;
        }
    }

    private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshState();

    /// <summary>The menus for this device. The static lists are rebuilt too; it is cheap and keeps one code path.</summary>
    private void RebuildOptions()
    {
        Fill(FanSpeeds, fanSpeedCycle, speed => new FanSpeedOption(speed, SetFanSpeed));
        Fill(PowerLimits, powerLimitCycle, limit => new PowerLimitOption(limit, SetPowerLimit));
        Fill(Temperatures, Enumerable.Range(MinimumTemperature, MaximumTemperature - MinimumTemperature + 1).Reverse().Select(t => (sbyte)t), t => new TemperatureOption(t, SetTemperature));

        meritFeatureCycle = Device == null ? [] : AvailableMeritFeatures(Device);
        swingModeCycle = Device == null ? [] : AvailableSwingModes(Device);
        Fill(MeritFeatures, meritFeatureCycle, feature => new MeritFeatureOption(feature, SetMeritFeature));
        Fill(SwingModes, swingModeCycle, mode => new SwingModeOption(mode, SetSwingMode));
        RefreshState();
        return;

        static void Fill<TOption, TValue>(ObservableCollection<TOption> options, IEnumerable<TValue> values, Func<TValue, TOption> create)
        {
            options.Clear();
            values.Select(create).Apply(options.Add);
        }
    }

    /// <summary>Re-announces what the view shows of the state and moves the check marks of the menus.</summary>
    private void RefreshState()
    {
        NotifyOfPropertyChange(nameof(DisplayTargetTemperature));
        NotifyOfPropertyChange(nameof(TargetTemperatureText));
        NotifyOfPropertyChange(nameof(IndoorTemperatureText));
        NotifyOfPropertyChange(nameof(OutdoorTemperatureText));

        var state = Device?.State;
        Mark(FanSpeeds, state?.FanSpeed);
        Mark(PowerLimits, state?.PowerLimit);
        Mark(MeritFeatures, state?.MeritFeaturesA);
        Mark(SwingModes, state?.SwingMode);
        Mark(Temperatures, state?.TargetTemperatureCelsius);
        return;

        static void Mark<TOption, TValue>(IEnumerable<TOption> options, TValue? current) where TOption : HvacOption<TValue> where TValue : struct
        {
            options.Apply(option => option.IsSelected = current.HasValue && option.Value.Equals(current.Value));
        }
    }

    private void NotifyCanSendChanged()
    {
        TogglePowerCommand.NotifyCanExecuteChanged();
        SetModeCommand.NotifyCanExecuteChanged();
        TemperatureUpCommand.NotifyCanExecuteChanged();
        TemperatureDownCommand.NotifyCanExecuteChanged();
        SetTemperatureCommand.NotifyCanExecuteChanged();
        CycleFanSpeedCommand.NotifyCanExecuteChanged();
        SetFanSpeedCommand.NotifyCanExecuteChanged();
        CyclePowerLimitCommand.NotifyCanExecuteChanged();
        SetPowerLimitCommand.NotifyCanExecuteChanged();
        CycleMeritFeatureCommand.NotifyCanExecuteChanged();
        SetMeritFeatureCommand.NotifyCanExecuteChanged();
        ToggleWifiLedCommand.NotifyCanExecuteChanged();
        CycleSwingModeCommand.NotifyCanExecuteChanged();
        SetSwingModeCommand.NotifyCanExecuteChanged();
    }

    #region Commands

    [RelayCommand(CanExecute = nameof(CanSend))]
    private Task TogglePower() => Send(state => state.IsTurnedOn = !Device!.State.IsTurnedOn);

    [RelayCommand(CanExecute = nameof(CanSend))]
    private Task SetMode(ToshibaHvacOperatingMode mode) => Send(state => state.Mode = mode);

    [RelayCommand(CanExecute = nameof(CanSend))]
    private Task TemperatureUp() => ChangeTemperature(1);

    [RelayCommand(CanExecute = nameof(CanSend))]
    private Task TemperatureDown() => ChangeTemperature(-1);

    /// <summary>Clamped to what the air conditioner accepts; the same value again is not sent.</summary>
    [RelayCommand(CanExecute = nameof(CanSend))]
    private Task SetTemperature(sbyte temperatureCelsius)
    {
        temperatureCelsius = sbyte.Clamp(temperatureCelsius, MinimumTemperature, MaximumTemperature);

        return Device?.State.TargetTemperatureCelsius == temperatureCelsius
            ? Task.CompletedTask
            : Send(state => state.TargetTemperatureCelsius = temperatureCelsius);
    }

    private Task ChangeTemperature(sbyte amount) => SetTemperature((sbyte)((Device?.State.TargetTemperatureCelsius ?? DefaultTemperature) + amount));

    [RelayCommand(CanExecute = nameof(CanSend))]
    private Task CycleFanSpeed() => Cycle(fanSpeedCycle, Device?.State.FanSpeed, SetFanSpeed);

    [RelayCommand(CanExecute = nameof(CanSend))]
    private Task SetFanSpeed(ToshibaHvacFanSpeed fanSpeed) => Send(state => state.FanSpeed = fanSpeed);

    [RelayCommand(CanExecute = nameof(CanSend))]
    private Task CyclePowerLimit() => Cycle(powerLimitCycle, Device?.State.PowerLimit, SetPowerLimit);

    [RelayCommand(CanExecute = nameof(CanSend))]
    private Task SetPowerLimit(byte powerLimit) => Send(state => state.PowerLimit = powerLimit);

    [RelayCommand(CanExecute = nameof(CanSend))]
    private Task CycleMeritFeature() => Cycle(meritFeatureCycle, Device?.State.MeritFeaturesA, SetMeritFeature);

    [RelayCommand(CanExecute = nameof(CanSend))]
    private Task SetMeritFeature(ToshibaHvacMeritFeaturesA meritFeature) => Send(state => state.MeritFeaturesA = meritFeature);

    [RelayCommand(CanExecute = nameof(CanSend))]
    private Task ToggleWifiLed() => Send(state => state.WifiLedStatus = Device!.State.WifiLedStatus == ToshibaHvacWifiLedStatus.On ? ToshibaHvacWifiLedStatus.Off : ToshibaHvacWifiLedStatus.On);

    [RelayCommand(CanExecute = nameof(CanSend))]
    private Task CycleSwingMode() => Cycle(swingModeCycle, Device?.State.SwingMode, SetSwingMode);

    [RelayCommand(CanExecute = nameof(CanSend))]
    private Task SetSwingMode(ToshibaHvacSwingMode swingMode) => Send(state => state.SwingMode = swingMode);

    /// <summary>The entry after the current one, or the first when the current value is not in the list.</summary>
    private static Task Cycle<T>(IReadOnlyList<T> values, T? current, Func<T, Task> set) where T : struct
    {
        if (values.Count == 0)
        {
            return Task.CompletedTask;
        }

        var index = current.HasValue ? values.ToList().IndexOf(current.Value) : -1;
        return set(values[(index + 1) % values.Count]);
    }

    #endregion

    /// <summary>
    /// Builds the delta from <paramref name="change"/>, sends it and waits for the server's verdict. Hub failures
    /// get a user-facing message; other exceptions reach <see cref="ViewModelBase.TaskExceptionHandler"/>.
    /// </summary>
    private Task Send(Action<ToshibaHvacStateData> change) => TaskExceptionHandler(async () =>
    {
        if (Device == null || DeviceKey == null || IsSending)
        {
            return;
        }

        var previousState = Device.State != null ? new ToshibaHvacStateData { StateData = [.. Device.State.StateData] } : null;
        var state = new ToshibaHvacStateData();
        change(state);
        IsSending = true;

        // No ConfigureAwait(false) here: IsSending raises CanExecuteChanged on the buttons, and Avalonia refuses
        // that from any thread but the UI thread. The continuation has to come back to where the click came from.
        try
        {
            ToshibaHvacCommandResult result;

            try
            {
                result = await commander.SendToshibaHvacCommand([DeviceKey], state);
            }
            catch (HubException ex)
            {
                RestoreState(previousState);
                IsSending = false;
                BusyText = null;
                await ex.ShowHubError();
                return;
            }
            catch
            {
                RestoreState(previousState);
                throw;
            }

            if (!result.IsSuccess)
            {
                await ShowUnconfirmed(result);
            }
        }
        finally
        {
            IsSending = false;
        }
    });

    private void RestoreState(ToshibaHvacStateData? previousState)
    {
        if (Device != null && previousState != null)
        {
            Device.State.StateData = [.. previousState.StateData];
        }
    }

    /// <summary>The message for a command that was sent but not echoed. Virtual so a test can catch it instead of a dialog.</summary>
    protected virtual Task ShowUnconfirmed(ToshibaHvacCommandResult result) => new MessageBox
    {
        Title = Loc.Warning,
        Text = string.Format(Loc.ToshibaHvacCommandNotConfirmed, Device?.DisplayName),
        Icon = new WarningIcon(),
        Buttons = [Loc.Ok],
    }.Show().AsTask();
}
