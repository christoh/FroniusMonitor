using System.Security.Cryptography;

namespace De.Hochstaetter.HomeAutomationClient.Views;

public partial class UiDemoView : UserControl
{
    public UiDemoView()
    {
        UiDemoViewModel viewModel;
        InitializeComponent();
        DataContext = viewModel = IoC.GetRegistered<UiDemoViewModel>();
        _ = viewModel.Initialize();
    }
}