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
}