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
    private readonly string deviceId;
    private Gen24ModbusSettings loadedSettings;

    public Gen24ModbusViewModel(string deviceId, Gen24ModbusSettings settings)
    {
        this.deviceId = deviceId;
        loadedSettings = settings;
        Settings = (Gen24ModbusSettings)settings.Clone();
        AttachTo(Settings);
        EnableTcp = Settings.Mode is ModbusSlaveMode.Tcp or ModbusSlaveMode.Both;
    }

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

    /// <summary>Empty unless the last Apply had something to say. Cleared as soon as anything is edited again.</summary>
    [ObservableProperty]
    public partial string? ResultText { get; set; }

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
        ResultText = null;
        NotifyAllVisibilities();
    }

    [RelayCommand]
    private Task Apply() => TaskExceptionHandler(async () =>
    {
        BusyText = Loc.Modbus;
        ResultText = null;

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

        var result = await webClient.SetGen24ModbusSettings(deviceId, Settings).ConfigureAwait(true);

        if (result.Status != HttpStatusCode.OK)
        {
            await ShowHttpError(result).ConfigureAwait(true);
            return;
        }

        // False means the server found no difference to what the inverter already had.
        ResultText = result.Payload is true ? Loc.SettingsSavedToInverter : Loc.NoSettingsChanged;
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
        ResultText = null;

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
