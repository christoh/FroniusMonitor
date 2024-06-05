namespace De.Hochstaetter.FroniusMonitor.Views;

public partial class BatteryDetailsView
{
    private readonly IDataCollectionService dataCollectionService;
    private readonly BatteryDetailsViewModel viewModel;

    public BatteryDetailsView(BatteryDetailsViewModel viewModel, IDataCollectionService dataCollectionService)
    {
        this.dataCollectionService = dataCollectionService;
        InitializeComponent();
        DataContext = this.viewModel = viewModel;

        dataCollectionService.NewDataReceived += OnNewDataReceived;
        Closed += (_, _) => { dataCollectionService.NewDataReceived -= OnNewDataReceived; };
        Loaded += (_, _) => { _ = viewModel.OnInitialize(); };
        OnNewDataReceived();
    }

    protected override ScaleTransform Scaler => WrapPanelScaler;

    private void OnNewDataReceived(object? _ = null, SolarDataEventArgs? __ = null)
    {
        viewModel.OnNewDataReceived(dataCollectionService.HomeAutomationSystem);
    }
}