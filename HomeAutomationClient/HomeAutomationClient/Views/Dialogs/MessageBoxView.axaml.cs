using Avalonia.Interactivity;

namespace De.Hochstaetter.HomeAutomationClient.Views.Dialogs;

public partial class MessageBoxView : UserControl, IDialogControl
{
    private MessageBoxViewModel? ViewModel => DataContext as MessageBoxViewModel;

    public MessageBoxView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _ = ViewModel?.Initialize();
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
