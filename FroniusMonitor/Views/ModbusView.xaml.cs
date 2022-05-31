namespace De.Hochstaetter.FroniusMonitor.Views
{
    public partial class ModbusView
    {
        public ModbusView(ModbusViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            Loaded += async (_, _) =>
            {
                viewModel.Dispatcher = Dispatcher;
                await viewModel.OnInitialize().ConfigureAwait(false);
            };
        }
    }
}
