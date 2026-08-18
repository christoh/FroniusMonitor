namespace De.Hochstaetter.HomeAutomationClient.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        DashboardViewModel viewModel;
        InitializeComponent();
        DataContext = viewModel = IoC.GetRegistered<DashboardViewModel>();
        _ = viewModel.Initialize();
    }
}
