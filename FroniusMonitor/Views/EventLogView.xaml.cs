namespace De.Hochstaetter.FroniusMonitor.Views;

public partial class EventLogView : IInverterScoped
{
    public EventLogView(EventLogViewModel viewModel, IWebClientService webClientService)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.View = this;
        WebClientService = webClientService;

        Loaded += async (_, _) =>
        {
            ViewModel.Dispatcher = Dispatcher;
            await ViewModel.OnInitialize().ConfigureAwait(false);
        };
    }

    public EventLogViewModel ViewModel => (EventLogViewModel)DataContext;
    public IWebClientService WebClientService { get; }
}