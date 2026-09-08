namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

/// <summary>
/// The event log tab of the inverter settings dialog, ported from <c>EventLogViewModel</c> of FroniusMonitor.
/// </summary>
/// <remarks>
/// <para>
/// The log is <b>not</b> part of the settings snapshot the dialog reads, and it is not read when the dialog opens
/// either: it is its own request to the inverter, of a few hundred entries, and most of the time the dialog is
/// opened to change a setting rather than to read the log. So it is fetched the first time the user actually
/// looks at the tab - <see cref="IsSelected"/> is what says that has happened - and then kept.
/// </para>
/// <para>
/// What an event code means is a translated string that lives on the inverter, and it is localized <b>here</b>
/// rather than by the server: the server would answer in whatever language it happens to run in. The client has
/// downloaded those translations already, in its own language - see <see cref="IGen24LocalizationService"/> -
/// and <see cref="Gen24Event.Message"/> is where the text goes.
/// </para>
/// </remarks>
public sealed partial class Gen24EventLogViewModel(Gen24SettingsDialogViewModel owner, string deviceId) : ViewModelBase
{
    private readonly IWebClientService webClient = IoC.GetRegistered<IWebClientService>();
    private readonly IGen24LocalizationService gen24Loc = IoC.GetRegistered<IGen24LocalizationService>();

    /// <inheritdoc cref="Gen24ModbusViewModel.BusyText"/>
    public override string? BusyText
    {
        get => owner.BusyText;
        set => owner.BusyText = value;
    }

    /// <summary>
    /// Whether this tab is the one on screen. Two way bound to <c>TabItem.IsSelected</c>, because reading the log
    /// is what looking at the tab means - see the remarks on the class.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>The log, newest first, or <see langword="null"/> until it has been read.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial IReadOnlyList<Gen24Event>? Events { get; set; }

    /// <summary>
    /// True once the log has been read and there is nothing in it, so the tab can say so rather than showing an
    /// empty table. An inverter that has never faulted is the good case, not an error.
    /// </summary>
    public bool IsEmpty => Events?.Count == 0;

    partial void OnIsSelectedChanged(bool value)
    {
        // The log is the one tab worth giving more room to - the other tabs are forms of a settled size, and
        // dragging one wider only grows its whitespace - so the dialog may be resized while this is the tab on
        // screen. A size the user has dragged to is kept when they leave the tab; only the grip goes.
        owner.Parameters.IsResizeable = value;

        if (value && Events is null)
        {
            // Nothing awaits this - the tab is drawn now and filled when the inverter answers. TaskExceptionHandler
            // inside Read is what makes that safe: a fire and forget task that throws has nobody to report to, and
            // in this app an exception that escapes takes the whole thing down.
            _ = Read();
        }
    }

    private Task Read() => TaskExceptionHandler(async () =>
    {
        BusyText = Loc.ReadingEventLog;
        var result = await webClient.GetGen24Events(deviceId).ConfigureAwait(true);

        if (result.Status != HttpStatusCode.OK || result.Payload is not { } events)
        {
            await ShowHttpError(result).ConfigureAwait(true);
            return;
        }

        events.Apply(froniusEvent => froniusEvent.Message = gen24Loc.GetEventDisplayName(froniusEvent.Code));
        Events = events;
    });
}
