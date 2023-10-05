namespace De.Hochstaetter.FroniusMonitor.Views;

public partial class EventLogView : IInverterScoped
{
    public EventLogView(EventLogViewModel viewModel, IGen24Service gen24Service)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.View = this;
        Gen24Service = gen24Service;

        Loaded += async (_, _) =>
        {
            ViewModel.Dispatcher = Dispatcher;
            await ViewModel.OnInitialize().ConfigureAwait(false);
        };
    }

    public EventLogViewModel ViewModel => (EventLogViewModel)DataContext;
    public IGen24Service Gen24Service { get; }
}