namespace De.Hochstaetter.FroniusMonitor.Views
{
    public partial class ModbusView : IInverterScoped
    {
        public ModbusView(ModbusViewModel viewModel, IWebClientService webClientService)
        {
            InitializeComponent();
            viewModel.View = this;
            DataContext = viewModel;
            WebClientService = webClientService;

            Loaded += async (_, _) =>
            {
                viewModel.Dispatcher = Dispatcher;
                await viewModel.OnInitialize().ConfigureAwait(false);
            };
        }

        public IWebClientService WebClientService { get; }
    }
}
