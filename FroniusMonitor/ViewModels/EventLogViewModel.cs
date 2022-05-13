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

    internal override async Task OnInitialize()
    {
        await base.OnInitialize().ConfigureAwait(false);
        Events = await webClientService.GetFroniusEvents().ConfigureAwait(false);
    }
}