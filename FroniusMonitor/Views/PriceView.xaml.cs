namespace De.Hochstaetter.FroniusMonitor.Views;

public partial class PriceView
{
    private readonly PriceViewModel viewModel;

    public PriceView(PriceViewModel viewModel)
    {
        InitializeComponent();
        DataContext = this.viewModel = viewModel;
        Loaded += async (_, _) => await viewModel.OnInitialize().ConfigureAwait(false);
        Closed += async (_, _) => await viewModel.CleanUp().ConfigureAwait(false);
    }

    protected override ScaleTransform Scaler => ScaleTransform;
}