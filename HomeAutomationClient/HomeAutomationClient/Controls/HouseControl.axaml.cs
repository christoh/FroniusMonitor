namespace De.Hochstaetter.HomeAutomationClient.Controls;

public partial class HouseControl : UserControl
{
    public HouseControl()
    {
        InitializeComponent();
        DataContext = IoC.TryGetRegistered<HouseViewModel>();
    }
}
