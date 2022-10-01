namespace De.Hochstaetter.FroniusMonitor.Views;

public partial class WattPilotSettingsView
{
    public WattPilotSettingsView(WattPilotSettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += async (_, _) =>
        {
            viewModel.Dispatcher = Dispatcher;
            await viewModel.OnInitialize().ConfigureAwait(false);
        };
    }

    private void OnChargingLogicChanged(object sender, SelectionChangedEventArgs e)
    {
        EcoSettings.Visibility =
            ChargingLogicBox.SelectedItem is ChargingLogic.NextTrip or ChargingLogic.Eco
                ? Visibility.Visible
                : Visibility.Collapsed;

        NextTripSettings.Visibility =
            ChargingLogicBox.SelectedItem is ChargingLogic.NextTrip
                ? Visibility.Visible
                : Visibility.Collapsed;
    }
}
