namespace De.Hochstaetter.HomeAutomationClient.Controls;

public partial class HouseControl : UserControl
{
    public HouseControl()
    {
        InitializeComponent();
        DataContext = IoC.TryGetRegistered<HouseViewModel>();

        // The house view model is one for the app and follows the update service the whole time; this is what tells
        // it whether the dashboard it sits on can be seen, so it can leave a report alone while it cannot.
        ViewVisibility.Follow(this);
    }
}
