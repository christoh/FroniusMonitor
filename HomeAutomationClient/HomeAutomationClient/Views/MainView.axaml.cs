using Avalonia.Interactivity;

namespace De.Hochstaetter.HomeAutomationClient.Views;

public partial class MainView : UserControl
{
    private MainViewModel viewModel;

    public MainView()
    {
        InitializeComponent();
        DataContext = viewModel = IoC.GetRegistered<MainViewModel>();
    }

    private void OnDialogCloseClick(object? sender, RoutedEventArgs e)
    {
        DialogOverlay.IsVisible = false;
    }
}