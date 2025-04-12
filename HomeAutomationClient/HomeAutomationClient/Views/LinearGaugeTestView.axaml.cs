using De.Hochstaetter.HomeAutomationClient.ViewModels;

namespace De.Hochstaetter.HomeAutomationClient.Views;

public partial class LinearGaugeTestView : UserControl
{
    private GaugeTestViewModel? ViewModel => DataContext as GaugeTestViewModel;

    public LinearGaugeTestView()
    {
        InitializeComponent();
        DataContext = IoC.TryGetRegistered<LinearGaugeTestViewModel>();
    }
}