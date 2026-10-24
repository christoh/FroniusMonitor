using De.Hochstaetter.HomeAutomationClient.Extensions;

namespace De.Hochstaetter.HomeAutomationClient.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        this.UseViewModel<MainViewModel>();
    }
}
