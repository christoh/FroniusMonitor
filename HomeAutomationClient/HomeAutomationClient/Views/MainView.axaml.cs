namespace De.Hochstaetter.HomeAutomationClient.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        MainViewModel? mainViewModel = IoC.TryGetRegistered<MainViewModel>();
        InitializeComponent();
        DataContext = mainViewModel;

        if (mainViewModel is { })
        {
            ViewModelBase.HandleTaskExceptions(mainViewModel.Initialize);
        }
    }
}