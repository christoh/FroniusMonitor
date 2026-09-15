namespace De.Hochstaetter.HomeAutomationClient.Views.Dialogs;

public partial class LoginView : UserControl, IDialogControl
{
    private LoginViewModel? ViewModel => DataContext as LoginViewModel;

    public LoginView()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _ = ViewModel?.Initialize();
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
