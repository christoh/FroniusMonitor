namespace De.Hochstaetter.HomeAutomationClient.Views;

public partial class GaugeTestView : UserControl
{
    private readonly GaugeTestViewModel viewModel = IoC.GetRegistered<GaugeTestViewModel>();

    public GaugeTestView()
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (_, _) => viewModel.Initialize();
    }
}