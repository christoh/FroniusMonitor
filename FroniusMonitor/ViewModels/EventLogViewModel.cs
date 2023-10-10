namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class EventLogViewModel : ViewModelBase
{
    private readonly IGen24Service gen24Service;

    public EventLogViewModel(IGen24Service gen24Service)
    {
        this.gen24Service = gen24Service;
    }

    private IOrderedEnumerable<Gen24Event>? events;

    public IOrderedEnumerable<Gen24Event>? Events
    {
        get => events;
        set => Set(ref events, value);
    }

    private string title = string.Empty;

    public string Title
    {
        get => title;
        set => Set(ref title, value);
    }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    internal override async Task OnInitialize()
    {
        try
        {
            Title = await gen24Service.GetUiString("EVENTLOG.TITLE").ConfigureAwait(false);
            await base.OnInitialize().ConfigureAwait(false);
            var inverterBaseSettings = await gen24Service.ReadGen24Entity<Gen24InverterSettings>("config/common").ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(inverterBaseSettings.SystemName))
            {
                Title += $" - {inverterBaseSettings.SystemName}";
            }

            Events = await gen24Service.GetFroniusEvents().ConfigureAwait(false);
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
