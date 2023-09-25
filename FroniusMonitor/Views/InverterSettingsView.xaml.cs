namespace De.Hochstaetter.FroniusMonitor.Views;

public partial class InverterSettingsView : IInverterScoped
{
    public InverterSettingsView(InverterSettingsViewModel viewModel, IWebClientService webClientService)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.View = this;
        WebClientService = webClientService;

        Loaded += async (_, _) =>
        {
            viewModel.Dispatcher = Dispatcher;
            await viewModel.OnInitialize().ConfigureAwait(false);
        };
    }

    public IWebClientService WebClientService { get; }
}