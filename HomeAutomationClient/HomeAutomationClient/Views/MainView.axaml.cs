using Avalonia.Interactivity;

namespace De.Hochstaetter.HomeAutomationClient.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        MainViewModel mainViewModel;
        InitializeComponent();
        DataContext = mainViewModel = IoC.GetRegistered<MainViewModel>();
        _ = mainViewModel.Initialize();
    }
}