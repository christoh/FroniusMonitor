namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class EventLogViewModel(IGen24Service gen24Service) : ViewModelBase
{
    public IOrderedEnumerable<Gen24Event>? Events
    {
        get;
        set => Set(ref field, value);
    }

    public string Title
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    internal override async Task OnInitialize()
    {
        try
        {
            Title = await gen24Service.GetUiString("EVENTLOG.TITLE").ConfigureAwait(false);
            await base.OnInitialize().ConfigureAwait(false);
            var inverterBaseSettings = await gen24Service.ReadGen24Entity<Gen24InverterSettings>("api/config/common").ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(inverterBaseSettings.SystemName))
            {
                Title += $" - {inverterBaseSettings.SystemName}";
            }

            var events = await gen24Service.GetFroniusEvents().ConfigureAwait(false);

            // The description of an event comes from a translation file the inverter serves, so it is read here
            // rather than by the model: this view model holds the service bound to the inverter being looked at,
            // and it can await the download instead of blocking the render thread on it once per visible row.
            foreach (var froniusEvent in events)
            {
                froniusEvent.Message = await gen24Service.GetEventDescription(froniusEvent.Code).ConfigureAwait(false);
            }

            Events = events;
        }
        catch (Exception ex)
        {
            ShowBox
            (
                string.Format(Resources.InverterCommReadError, ex is TaskCanceledException ? Loc.InverterTimeout : ex.Message),
                    ex.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error
            );

            Close();
        }
    }
}
