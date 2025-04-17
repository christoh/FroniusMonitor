namespace De.Hochstaetter.HomeAutomationClient.Views;

public partial class LinearGaugeTestView : UserControl
{
    private readonly LinearGaugeTestViewModel viewModel= IoC.GetRegistered<LinearGaugeTestViewModel>();

    public LinearGaugeTestView()
    {
        ;
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (_, _) => viewModel.Initialize();
    }
}