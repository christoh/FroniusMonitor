namespace De.Hochstaetter.FroniusMonitor.Views;

public partial class EventLogView
{
    public EventLogView(EventLogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += async (s, e) =>
        {
            ViewModel.Dispatcher = Dispatcher;
            await ViewModel.OnInitialize().ConfigureAwait(false);
        };
    }

    public EventLogViewModel ViewModel => (EventLogViewModel)DataContext;
}