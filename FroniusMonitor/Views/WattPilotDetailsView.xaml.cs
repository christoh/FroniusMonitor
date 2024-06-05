namespace De.Hochstaetter.FroniusMonitor.Views;

public partial class WattPilotDetailsView
{
    private readonly WattPilotDetailsViewModel viewModel;
    private readonly IWattPilotService wattPilotService;

    public WattPilotDetailsView(WattPilotDetailsViewModel viewModel, IWattPilotService wattPilotService)
    {
        InitializeComponent();
        DataContext = this.viewModel = viewModel;
        this.wattPilotService = wattPilotService;
    }

    protected override ScaleTransform Scaler => WrapPanelScaler;

    private void OnSettingsClicked(object sender, RoutedEventArgs e)
    {
        IoC.TryGetRegistered<MainWindow>()?.GetView<WattPilotSettingsView>().Focus();
    }

    private async void OnRebootWattPilotClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            await viewModel.WattPilotService.RebootWattPilot().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Loc.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenChargingLogClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            wattPilotService.OpenChargingLog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(Window.GetWindow(this)!, ex.Message, Loc.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}