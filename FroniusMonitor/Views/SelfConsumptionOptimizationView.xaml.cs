namespace De.Hochstaetter.FroniusMonitor.Views;

public partial class SelfConsumptionOptimizationView : IInverterScoped
{
    public SelfConsumptionOptimizationView(SelfConsumptionOptimizationViewModel viewModel, IGen24Service gen24Service)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.View = this;
        Gen24Service = gen24Service;

        Loaded += async (_, _) =>
        {
            viewModel.Dispatcher = Dispatcher;
            await viewModel.OnInitialize().ConfigureAwait(false);
        };
    }

    public IGen24Service Gen24Service { get; }
}
