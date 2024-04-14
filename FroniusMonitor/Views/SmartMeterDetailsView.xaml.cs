namespace De.Hochstaetter.FroniusMonitor.Views;

public partial class SmartMeterDetailsView
{
    private bool isLoaded;

    public SmartMeterDetailsView(SmartMeterDetailsViewModel viewModel, IDataCollectionService dataCollectionService)
    {
        InitializeComponent();
        DataContext = ViewModel = viewModel;

        Loaded += async (_, _) =>
        {
            if (isLoaded)
            {
                return;
            }

            isLoaded = true;
            await viewModel.OnInitialize().ConfigureAwait(false);
            dataCollectionService.NewDataReceived += OnNewDataReceived;
            OnNewDataReceived(this, new SolarDataEventArgs(dataCollectionService.HomeAutomationSystem));
        };

        Closed += (_, _) => dataCollectionService.NewDataReceived -= OnNewDataReceived;
    }

    private SmartMeterDetailsViewModel ViewModel { get; }

    private void OnNewDataReceived(object? o, SolarDataEventArgs e)
    {
        if (e.HomeAutomationSystem != null)
        {
            ViewModel.OnNewDataReceived(e.HomeAutomationSystem);
        }
    }
}
