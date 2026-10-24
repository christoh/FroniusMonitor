namespace De.Hochstaetter.HomeAutomationClient.Views.Dialogs;

public partial class LoginView : DialogView
{
    private LoginViewModel? ViewModel => DataContext as LoginViewModel;

    public LoginView()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                // One Ok for both modes, so Enter needs to know nothing about which of them is showing.
                OkButton.Focus();
                ViewModel?.OkCommand.Execute(null);
                break;
        }
    }
}
