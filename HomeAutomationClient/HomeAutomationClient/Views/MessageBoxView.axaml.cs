using Avalonia.Interactivity;

namespace De.Hochstaetter.HomeAutomationClient.Views;

public partial class MessageBoxView : UserControl, IDialogControl
{
    private MessageBoxViewModel ViewModel => (MessageBoxViewModel)DataContext!;
    
    public MessageBoxView()
    {
        InitializeComponent();
    }

    private void OnButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Content: string buttonText })
        {
            var index = ViewModel.Parameters!.Buttons.IndexOf(buttonText);
            ViewModel.OnButtonClicked(index);
        }
    }
}