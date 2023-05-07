using De.Hochstaetter.Fronius.Models.Settings;

namespace De.Hochstaetter.FroniusMonitor.Views
{
    public partial class SettingsView
    {
        public SettingsView(SettingsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            Loaded += async (_, _) =>
            {
                viewModel.Dispatcher = Dispatcher;
                await viewModel.OnInitialize().ConfigureAwait(false);
            };
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

    }
}
