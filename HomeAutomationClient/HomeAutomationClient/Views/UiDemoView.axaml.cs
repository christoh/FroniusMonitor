using System.Security.Cryptography;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Styling;

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