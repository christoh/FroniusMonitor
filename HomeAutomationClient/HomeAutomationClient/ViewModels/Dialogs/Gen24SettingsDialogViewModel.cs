using De.Hochstaetter.Fronius.Models.Gen24.Settings;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

/// <summary>
/// The inverter settings dialog: the four settings views of FroniusMonitor - self consumption, inverter settings,
/// event log and Modbus - as four tabs of one dialog.
/// </summary>
/// <remarks>
/// <para>
/// The snapshot is read once, here, and handed to the tabs, so all four show the same moment in time and the
/// inverter is asked once rather than four times. It may go stale while the dialog is open and that is fine: every
/// write goes through the server, which reads the inverter again and applies only the difference.
/// </para>
/// <para>
/// Nothing here goes over the SignalR hub. The hub carries the live device stream in one direction; settings are
/// requests with an answer, so they go over https through <see cref="IWebClientService"/>.
/// </para>
/// </remarks>
public sealed partial class Gen24SettingsDialogViewModel(DialogParameters parameters)
    : DialogBase<DialogParameters, bool, Gen24SettingsDialogView>(parameters), ITabHost
{
    private readonly IWebClientService webClient = IoC.GetRegistered<IWebClientService>();
    private bool isInitialized;

    /// <summary>The key of the inverter, as <see cref="IUpdateService.DevicesWithSettings"/> knows it.</summary>
    public required string DeviceId { get; init; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowModbus))]
    public partial Gen24ModbusViewModel? Modbus { get; set; }

    /// <summary>
    /// The event log tab. Unlike the others it does not come out of the snapshot - the log is its own request,
    /// which the tab makes the first time the user looks at it - so this is here from the start.
    /// </summary>
    [ObservableProperty]
    public partial Gen24EventLogViewModel? EventLog { get; set; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowSelfConsumption))]
    public partial Gen24SelfConsumptionViewModel? SelfConsumption { get; set; }

    /// <inheritdoc cref="ShowModbus"/>
    public bool ShowSelfConsumption => !IsLoaded || SelfConsumption is not null;

    /// <summary>True once the snapshot has arrived.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowModbus))]
    public partial bool IsLoaded { get; set; }

    /// <summary>
    /// What the toast says. It belongs to the dialog rather than to a tab, the same way
    /// <see cref="DialogBase{TParameters,TResult,TBody}.BusyText"/> does: the room for it is in the button row of
    /// the dialog, next to Cancel, and a tab squeezed into its own row has hardly any. A tab writes to it through
    /// <c>Gen24ModbusViewModel.ToastText</c>, and the <c>Toast</c> control clears it again once it has faded.
    /// </summary>
    [ObservableProperty]
    public partial string? ToastText { get; set; }

    /// <summary>
    /// The tab stays in place while the settings are still being read, so the set of tabs does not change under
    /// the user a second after the dialog opened. Once they have arrived it is only there if the inverter has a
    /// Modbus configuration at all.
    /// </summary>
    public bool ShowModbus => !IsLoaded || Modbus is not null;

    /// <summary>
    /// Reads the snapshot, once. The view calls this from <c>OnDataContextChanged</c>, and that fires again every
    /// time this dialog is re-attached - which happens whenever a message box has opened and closed over it, an
    /// error from one of the tabs for instance. Without the guard the inverter would be read again at that point
    /// and the busy overlay would come back up over a dialog the user is working in.
    /// </summary>
    public override Task Initialize() => TaskExceptionHandler(async () =>
    {
        if (isInitialized)
        {
            return;
        }

        // Set before the first await, so a second call cannot get past the guard while the first is still running.
        isInitialized = true;

        // The event log needs nothing from the snapshot, so its tab is live before the inverter has answered.
        EventLog = new Gen24EventLogViewModel(this, DeviceId);

        await base.Initialize().ConfigureAwait(true);
        BusyText = Loc.ReadingInverterSettings;

        var result = await webClient.GetGen24Settings(DeviceId).ConfigureAwait(true);

        if (result.Status != HttpStatusCode.OK || result.Payload is not { } snapshot)
        {
            await ShowHttpError(result).ConfigureAwait(true);
            Close();
            return;
        }

        // The title is not set here on purpose: the dialog frame reads Parameters.Title once, while the dialog is
        // being put up, which is before this has run. MainViewModel.Settings therefore supplies it.

        // Inverter settings is not ported yet; it gets its view model here as it arrives. The event log is set up
        // above, because it does not read from the snapshot.
        Modbus = snapshot.ModbusSettings is { } modbusSettings ? new Gen24ModbusViewModel(this, DeviceId, modbusSettings) : null;

        SelfConsumption = snapshot.BatterySettings is { } batterySettings
            ? new Gen24SelfConsumptionViewModel(this, DeviceId, batterySettings, snapshot.ChargingRules)
            : null;

        // The limits of the state of charge sliders come from the live battery rather than from the snapshot, so
        // they are read after the tab exists and are simply the whole range where there is no battery to ask.
        SelfConsumption?.ReadBatteryLimits();

        IsLoaded = true;
    });

    [RelayCommand]
    private void CloseDialog()
    {
        Result = true;
        Close();
    }

    /// <summary>
    /// The close box of the dialog frame ends up here, and it does exactly what the Cancel button does. Nothing
    /// has to be unwound either way: a tab writes to the inverter when the user applies it, never on the way out.
    /// </summary>
    /// <remarks>
    /// <see cref="Close"/> has to be called from here. Setting only <see cref="DialogBase{T,TResult,TBody}.Result"/>
    /// leaves the dialog on screen and <c>ShowDialogAsync</c> waiting, so the close box would do nothing at all.
    /// </remarks>
    public override Task AbortAsync()
    {
        CloseDialog();
        return Task.CompletedTask;
    }
}
