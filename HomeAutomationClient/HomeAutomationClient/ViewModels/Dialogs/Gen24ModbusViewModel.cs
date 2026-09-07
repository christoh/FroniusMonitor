using System.ComponentModel;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Gen24.Settings;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

/// <summary>
/// The Modbus tab of the inverter settings dialog, ported from <c>ModbusViewModel</c> of FroniusMonitor.
/// </summary>
/// <remarks>
/// <para>
/// Every "is this group visible" rule lives here as a bindable property rather than in a multi binding in the
/// view, which is what the view model rule of this repo asks for and what Avalonia would make awkward anyway.
/// </para>
/// <para>
/// Nothing is written to the inverter from here. <see cref="IWebClientService.SetGen24ModbusSettings"/> hands the
/// whole settings object to the server over https, and the server works out the delta against what the inverter
/// currently holds. So this view model never has to keep the old settings for the sake of the write - it keeps
/// them only so that Undo works.
/// </para>
/// </remarks>
public sealed partial class Gen24ModbusViewModel : ViewModelBase
{
    private readonly IWebClientService webClient = IoC.GetRegistered<IWebClientService>();
    private readonly Gen24SettingsDialogViewModel owner;
    private readonly string deviceId;
    private Gen24ModbusSettings loadedSettings;

    public Gen24ModbusViewModel(Gen24SettingsDialogViewModel owner, string deviceId, Gen24ModbusSettings settings)
    {
        this.owner = owner;
        this.deviceId = deviceId;
        loadedSettings = settings;
        Settings = (Gen24ModbusSettings)settings.Clone();
        AttachTo(Settings);
        EnableTcp = Settings.Mode is ModbusSlaveMode.Tcp or ModbusSlaveMode.Both;
    }

    /// <summary>
    /// A tab has no busy indicator of its own - the dialog around it has one. Proxying it means the guard in
    /// <see cref="ViewModelBase.TaskExceptionHandler"/> clears the indicator the user is actually looking at,
    /// rather than a property nothing is bound to.
    /// </summary>
    public override string? BusyText
    {
        get => owner.BusyText;
        set => owner.BusyText = value;
    }

    /// <summary>
    /// What the user has typed that the settings refused, one message per field, and only for fields that are on
    /// screen. The view fills this in: which controls exist and which of them are visible is not something a view
    /// model can know, and it is the controls that hold the rejected text.
    /// </summary>
    public Func<IReadOnlyList<string>>? GetInvalidFields { get; set; }

    public IReadOnlyList<EnumListItemModel<ModbusInterfaceRole>> InterfaceRoles { get; } =
        [.. Enum.GetValues<ModbusInterfaceRole>().Select(r => new EnumListItemModel<ModbusInterfaceRole> { Value = r })];

    public IReadOnlyList<EnumListItemModel<ModbusParity>> Parities { get; } =
        [.. Enum.GetValues<ModbusParity>().Select(p => new EnumListItemModel<ModbusParity> { Value = p })];

    public IReadOnlyList<EnumListItemModel<SunspecMode>> SunspecModes { get; } =
        [.. Enum.GetValues<SunspecMode>().Select(m => new EnumListItemModel<SunspecMode> { Value = m })];

    [ObservableProperty]
    public partial Gen24ModbusSettings Settings { get; set; }

    /// <summary>
    /// The RTU roles can take the inverter off the bus, so they stay disabled until the user says they mean it.
    /// </summary>
    [ObservableProperty]
    public partial bool EnableDanger { get; set; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowAllowControl), nameof(ShowRestrictControl), nameof(ShowAllowedIp), nameof(ShowCommonSlaveSettings))]
    public partial bool EnableTcp { get; set; }

    /// <summary>What the toast says after an Apply. The toast clears it again once it has faded.</summary>
    [ObservableProperty]
    public partial string? ToastText { get; set; }

    public bool IsRtuSlave => Settings.Rtu0 == ModbusInterfaceRole.Slave || Settings.Rtu1 == ModbusInterfaceRole.Slave;

    public bool ShowAllowControl => EnableTcp;

    public bool ShowRestrictControl => EnableTcp && Settings.AllowControl is true;

    public bool ShowAllowedIp => ShowRestrictControl && Settings.RestrictControl is true;

    /// <summary>Meter address, SunSpec address and SunSpec mode only matter while the inverter is a slave at all.</summary>
    public bool ShowCommonSlaveSettings => IsRtuSlave || EnableTcp;

    [RelayCommand]
    private void Undo()
    {
        DetachFrom(Settings);
        Settings = (Gen24ModbusSettings)loadedSettings.Clone();
        AttachTo(Settings);
        EnableTcp = Settings.Mode is ModbusSlaveMode.Tcp or ModbusSlaveMode.Both;
        ToastText = null;
        NotifyAllVisibilities();
    }

    [RelayCommand]
    private Task Apply() => TaskExceptionHandler(async () =>
    {
        ToastText = null;

        // A setter of the settings throws on a value it will not take, so the binding keeps the text in the box,
        // marks it, and leaves the model as it was. Nothing is therefore wrong with the model at this point - the
        // fields are the only place that knows, which is why the view has to be asked.
        if (GetInvalidFields?.Invoke() is { Count: > 0 } invalidFields)
        {
            await new MessageBox
            {
                Text = $"{Loc.PleaseCorrectErrors}:",
                ItemList = [.. invalidFields],
                Title = Loc.Error,
                Buttons = [Loc.Ok],
                Icon = new ErrorIcon(),
            }.Show().ConfigureAwait(true);

            return;
        }

        BusyText = string.Format(CultureInfo.CurrentCulture, Loc.SavingSettings, Loc.Modbus);

        // The same rules FroniusMonitor applies before it writes: the mode follows from what the user enabled, and
        // an inverter that is not a slave anywhere has no address on the bus.
        var isRtuSlave = IsRtuSlave;
        Settings.InverterAddress = !isRtuSlave && !EnableTcp ? (byte)0 : (byte)1;

        Settings.Mode = (EnableTcp, isRtuSlave) switch
        {
            (true, true) => ModbusSlaveMode.Both,
            (true, false) => ModbusSlaveMode.Tcp,
            (false, true) => ModbusSlaveMode.Rtu,
            _ => ModbusSlaveMode.Off,
        };

        // Nothing to send, so nothing is sent - the same thing the dialog of FroniusMonitor does. The delta the
        // server works out is the one that counts, because it is taken against what the inverter holds right now;
        // this one only answers whether the user changed anything since the dialog read the settings. It has to be
        // taken after the mode above, which is derived rather than edited.
        if (!Settings.GetToken(loadedSettings).HasValues)
        {
            BusyText = null;

            await new MessageBox
            {
                Text = Loc.NoSettingsChanged,
                Title = Loc.Warning,
                Buttons = [Loc.Ok],
                Icon = new WarningIcon(),
            }.Show().ConfigureAwait(true);

            return;
        }

        var result = await webClient.SetGen24ModbusSettings(deviceId, Settings).ConfigureAwait(true);

        if (result.Status != HttpStatusCode.OK)
        {
            await ShowHttpError(result).ConfigureAwait(true);
            return;
        }

        // False means the server found no difference to what the inverter already had.
        ToastText = result.Payload is true ? Loc.SettingsSavedToInverter : Loc.NoSettingsChanged;
        loadedSettings = (Gen24ModbusSettings)Settings.Clone();
    });

    private void AttachTo(Gen24ModbusSettings settings) => settings.PropertyChanged += OnSettingsPropertyChanged;

    private void DetachFrom(Gen24ModbusSettings settings) => settings.PropertyChanged -= OnSettingsPropertyChanged;

    /// <summary>
    /// The visibility rules read properties of <see cref="Settings"/>, and a change there is a change of this view
    /// model as far as the view is concerned.
    /// </summary>
    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        ToastText = null;

        switch (e.PropertyName)
        {
            case nameof(Gen24ModbusSettings.Rtu0):
            case nameof(Gen24ModbusSettings.Rtu1):
                NotifyOfPropertyChange(nameof(IsRtuSlave));
                NotifyOfPropertyChange(nameof(ShowCommonSlaveSettings));
                break;

            case nameof(Gen24ModbusSettings.AllowControl):
                NotifyOfPropertyChange(nameof(ShowRestrictControl));
                NotifyOfPropertyChange(nameof(ShowAllowedIp));
                break;

            case nameof(Gen24ModbusSettings.RestrictControl):
                NotifyOfPropertyChange(nameof(ShowAllowedIp));
                break;
        }
    }

    private void NotifyAllVisibilities()
    {
        NotifyOfPropertyChange(nameof(IsRtuSlave));
        NotifyOfPropertyChange(nameof(ShowAllowControl));
        NotifyOfPropertyChange(nameof(ShowRestrictControl));
        NotifyOfPropertyChange(nameof(ShowAllowedIp));
        NotifyOfPropertyChange(nameof(ShowCommonSlaveSettings));
    }
}
