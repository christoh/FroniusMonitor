namespace De.Hochstaetter.FroniusMonitor.Views;

public partial class InverterSettingsView : IHaveWebClientService
{
    private readonly InverterSettingsViewModel viewModel;

    public InverterSettingsView(InverterSettingsViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        DataContext = viewModel;

        Loaded += async (_, _) =>
        {
            viewModel.Dispatcher = Dispatcher;
            await viewModel.OnInitialize().ConfigureAwait(false);
        };
    }

    public IWebClientService WebClientService
    {
        get => viewModel.WebClientService;
        set => viewModel.WebClientService = value;
    }
}