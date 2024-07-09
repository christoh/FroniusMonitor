namespace De.Hochstaetter.FroniusMonitor.Views;

public partial class ModbusView : IInverterScoped
{
    public ModbusView(ModbusViewModel viewModel, IGen24Service gen24Service)
    {
            InitializeComponent();
            viewModel.View = this;
            DataContext = viewModel;
            Gen24Service = gen24Service;

            Loaded += async (_, _) =>
            {
                viewModel.Dispatcher = Dispatcher;
                await viewModel.OnInitialize().ConfigureAwait(false);
            };
        }

    public IGen24Service Gen24Service { get; }
}