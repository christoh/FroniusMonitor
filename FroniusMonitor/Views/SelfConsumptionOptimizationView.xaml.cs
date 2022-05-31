namespace De.Hochstaetter.FroniusMonitor.Views;

public partial class SelfConsumptionOptimizationView
{
    public SelfConsumptionOptimizationView(SelfConsumptionOptimizationViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += async (_, _) =>
        {
            ViewModel.Dispatcher = Dispatcher;
            await ViewModel.OnInitialize().ConfigureAwait(false);
        };
    }

    public SelfConsumptionOptimizationViewModel ViewModel => (SelfConsumptionOptimizationViewModel)DataContext;
}