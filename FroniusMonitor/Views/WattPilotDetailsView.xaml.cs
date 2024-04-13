namespace De.Hochstaetter.FroniusMonitor.Views
{
    public partial class WattPilotDetailsView
    {
        public WattPilotDetailsView(WattPilotDetailsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
