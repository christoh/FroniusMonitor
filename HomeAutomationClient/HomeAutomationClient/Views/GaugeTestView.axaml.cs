namespace De.Hochstaetter.HomeAutomationClient.Views;

public partial class GaugeTestView : UserControl
{
    private GaugeTestViewModel? ViewModel => DataContext as GaugeTestViewModel;

    public GaugeTestView()
    {
        InitializeComponent();
        DataContext = IoC.TryGetRegistered<GaugeTestViewModel>();
    }
}