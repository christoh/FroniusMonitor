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
/// <para>
/// The two bus addresses are numbers, so the text boxes bind to <see cref="MeterAddressText"/> and
/// <see cref="SunSpecAddressText"/> rather than to the settings, and <see cref="Apply"/> copies them back. That is
/// the rule for text input in <c>.claude/rules/ViewModelsForInteractionLogic.md</c>, and everything else here
/// follows from it: whatever the user types is stored, so it validates, it raises a change, and Undo reaches the
/// box. Bound straight to a <c>byte?</c>, a letter or an emptied box is refused by the binding itself - the value
/// never changes, nothing notifies, and there is nothing for Undo to push.
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
        CopyFromSettings();
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

    /// <summary>
    /// The Modbus address of the smart meter, as the text box holds it. A string, not the <c>byte?</c> of the
    /// settings - see the remarks on this class. The rule is here rather than on the settings property because
    /// this is what the user edits; the settings carry the same one for the sake of the server, and both take
    /// their limits from <see cref="Gen24ModbusSettings"/> so that they cannot drift apart.
    /// </summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MinMaxInt(Gen24ModbusSettings.MinMeterAddress, Gen24ModbusSettings.MaxMeterAddress,
        MessageResourceKey = nameof(Loc.MeterAddressError), AllowEmpty = false)]
    public partial string? MeterAddressText { get; set; }

    /// <summary>
    /// The string control address, as the text box holds it. See <see cref="MeterAddressText"/>.
    /// </summary>
    /// <remarks>
    /// It may be left empty, which is why <c>AllowEmpty</c> stays at its default here: only a Tauro has string
    /// controllers, so an inverter with none has nothing to put in the box. An empty field is then left out of
    /// the delta the server works out, so it never overwrites what the inverter holds - it means "not mine to
    /// say", not "clear it".
    /// </remarks>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MinMaxInt(Gen24ModbusSettings.MinSunSpecAddress, Gen24ModbusSettings.MaxSunSpecAddress,
        MessageResourceKey = nameof(Loc.SunspecAddressError))]
    public partial string? SunSpecAddressText { get; set; }

    public bool IsRtuSlave => Settings.Rtu0 == ModbusInterfaceRole.Slave || Settings.Rtu1 == ModbusInterfaceRole.Slave;

    public bool ShowAllowControl => EnableTcp;

    public bool ShowRestrictControl => EnableTcp && Settings.AllowControl is true;

    public bool ShowAllowedIp => ShowRestrictControl && Settings.RestrictControl is true;

    /// <summary>Meter address, SunSpec address and SunSpec mode only matter while the inverter is a slave at all.</summary>
    public bool ShowCommonSlaveSettings => IsRtuSlave || EnableTcp;

    /// <summary>
    /// Unconditionally back to the settings the dialog started from - what it read from the inverter, or what the
    /// last successful Apply wrote there. Every field, whatever it currently holds.
    /// </summary>
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

        // Everything the user can have got wrong is in one of these two: the settings hold what the check boxes
        // and the combo boxes wrote, and this view model holds the text of the boxes the user types into. Both
        // validate through the same rules - see Fronius/Validators - so both are asked the same way.
        IReadOnlyList<string> errors =
        [
            .. Settings.GetErrors().Concat(GetErrors())
                .Select(error => error.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Select(message => message!)
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

        BusyText = string.Format(CultureInfo.CurrentCulture, Loc.SavingSettings, Loc.Modbus);
        CopyToSettings();

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

        // What was just written is what Undo goes back to from now on. Resetting from it also puts the boxes in
        // order, so a "0200" the user typed reads "200" once it has been saved.
        loadedSettings = (Gen24ModbusSettings)Settings.Clone();
        Reset();

        // After the reset: it clears the toast of the settings it replaces.
        // False means the server found no difference to what the inverter already had.
        ToastText = result.Payload is true ? Loc.SettingsSavedToInverter : Loc.NoSettingsChanged;
    });

    /// <summary>
    /// Everything back to <c>loadedSettings</c>: the settings object, the text of every box, and the state the
    /// visibility rules read. A fresh settings object also brings a fresh validation state, so the red frames go
    /// with it.
    /// </summary>
    private void Reset()
    {
        DetachFrom(Settings);
        Settings = (Gen24ModbusSettings)loadedSettings.Clone();
        AttachTo(Settings);
        CopyFromSettings();
    }

    /// <summary>The numbers of the settings into the strings the boxes are bound to.</summary>
    private void CopyFromSettings()
    {
        MeterAddressText = NumericText.Of(Settings.MeterAddress);
        SunSpecAddressText = NumericText.Of(Settings.SunSpecAddress);
        EnableTcp = Settings.Mode is ModbusSlaveMode.Tcp or ModbusSlaveMode.Both;
        NotifyAllVisibilities();
    }

    /// <summary>
    /// And back again, on the way to the inverter. Only ever called once the rules have passed, so the text is
    /// known to be a number in range.
    /// </summary>
    private void CopyToSettings()
    {
        Settings.MeterAddress = (byte?)NumericText.ToInteger(MeterAddressText);
        Settings.SunSpecAddress = (byte?)NumericText.ToInteger(SunSpecAddressText);
    }

    /// <summary>
    /// A field the user cannot see must not stop them saving, so anything that is both refused and off screen goes
    /// back to the value the dialog read from the inverter. The dialog of FroniusMonitor took the same view; it
    /// just had to find the fields by walking the visual tree and reflecting over the binding to get there.
    /// </summary>
    private void RestoreRefusedFieldsThatAreHidden()
    {
        if (!ShowAllowedIp && Settings.GetErrors(nameof(Gen24ModbusSettings.IpAddress)).Any())
        {
            Settings.IpAddress = loadedSettings.IpAddress;
        }

        if (ShowCommonSlaveSettings)
        {
            return;
        }

        if (GetErrors(nameof(MeterAddressText)).Any())
        {
            MeterAddressText = NumericText.Of(loadedSettings.MeterAddress);
        }

        if (GetErrors(nameof(SunSpecAddressText)).Any())
        {
            SunSpecAddressText = NumericText.Of(loadedSettings.SunSpecAddress);
        }
    }

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
