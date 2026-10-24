namespace De.Hochstaetter.HomeAutomationClient.Views.Dialogs;

public partial class MessageBoxView : DialogView
{
    private MessageBoxViewModel? ViewModel => DataContext as MessageBoxViewModel;

    public MessageBoxView()
    {
        InitializeComponent();
    }

    private void OnButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Content: string buttonText } && ViewModel != null)
        {
            var index = ViewModel.Parameters?.Buttons.IndexOf(buttonText) ?? -1;
            ViewModel.OnButtonClicked(index);
        }
    }
}
