namespace De.Hochstaetter.FroniusMonitor.Views
{
    public partial class ModbusView : Window
    {
        public ModbusView(ModbusViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            Loaded += async (s, e) =>
            {
                viewModel.Dispatcher = Dispatcher;
                await viewModel.OnInitialize().ConfigureAwait(false);
            };
        }
    }
}
