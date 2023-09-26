namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class EventLogViewModel : ViewModelBase
{
    private readonly IWebClientService webClientService;

    public EventLogViewModel(IWebClientService webClientService)
    {
        this.webClientService = webClientService;
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
        Title = await webClientService.GetUiString("EVENTLOG.TITLE").ConfigureAwait(false);
        await base.OnInitialize().ConfigureAwait(false);
        var inverterBaseSettings = await webClientService.ReadGen24Entity<Gen24InverterSettings>("config/common").ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(inverterBaseSettings.SystemName))
        {
            Title += $" - {inverterBaseSettings.SystemName}";
        }

        Events = await webClientService.GetFroniusEvents().ConfigureAwait(false);
    }
}