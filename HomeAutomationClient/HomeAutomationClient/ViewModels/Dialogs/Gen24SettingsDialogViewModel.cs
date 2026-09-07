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
    : DialogBase<DialogParameters, bool, Gen24SettingsDialogView>(parameters)
{
    private readonly IWebClientService webClient = IoC.GetRegistered<IWebClientService>();

    /// <summary>The key of the inverter, as <see cref="IUpdateService.DevicesWithSettings"/> knows it.</summary>
    public required string DeviceId { get; init; }

    [ObservableProperty]
    public partial Gen24ModbusViewModel? Modbus { get; set; }

    /// <summary>
    /// True once the snapshot has arrived. The tabs have no content to bind to before that.
    /// </summary>
    [ObservableProperty]
    public partial bool IsLoaded { get; set; }

    public override Task Initialize() => TaskExceptionHandler(async () =>
    {
        await base.Initialize().ConfigureAwait(true);
        BusyText = Loc.GetInverterLocalization;

        var result = await webClient.GetGen24Settings(DeviceId).ConfigureAwait(true);

        if (result.Status != HttpStatusCode.OK || result.Payload is not { } snapshot)
        {
            await ShowHttpError(result).ConfigureAwait(true);
            Close();
            return;
        }

        // The title is not set here on purpose: the dialog frame reads Parameters.Title once, while the dialog is
        // being put up, which is before this has run. MainViewModel.Settings therefore supplies it.

        // Only the Modbus tab is ported so far. The other three get their view models here as they arrive.
        Modbus = snapshot.ModbusSettings is { } modbusSettings ? new Gen24ModbusViewModel(DeviceId, modbusSettings) : null;
        IsLoaded = true;
    });

    [RelayCommand]
    private void CloseDialog()
    {
        Result = true;
        Close();
    }

    /// <summary>
    /// Nothing to unwind: every tab writes to the inverter when the user applies it, never on the way out.
    /// </summary>
    public override Task AbortAsync()
    {
        Result = false;
        return Task.CompletedTask;
    }
}
