namespace De.Hochstaetter.FroniusMonitor.Views;

public partial class PriceView
{
    private readonly PriceViewModel viewModel;

    public PriceView(PriceViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.OnInitialize().ConfigureAwait(false);
        Closed += async (_, _) => await viewModel.CleanUp().ConfigureAwait(false);
    }

    protected override ScaleTransform Scaler => ScaleTransform;

    private void OnConsumptionViewChecked(object sender, RoutedEventArgs e) => viewModel.PriceDisplay = ElectricityPriceDisplay.Buy;

    private void OnMarketViewChecked(object sender, RoutedEventArgs e)=>viewModel.PriceDisplay= ElectricityPriceDisplay.Market;
}