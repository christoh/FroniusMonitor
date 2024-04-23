namespace De.Hochstaetter.FroniusMonitor.Views
{
    public partial class WattPilotDetailsView
    {
        private readonly WattPilotDetailsViewModel viewModel;
        public WattPilotDetailsView(WattPilotDetailsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = this.viewModel = viewModel;
        }

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

    }
}
