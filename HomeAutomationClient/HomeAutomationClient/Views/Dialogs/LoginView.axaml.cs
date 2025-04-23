using Avalonia.Interactivity;

namespace De.Hochstaetter.HomeAutomationClient.Views.Dialogs;

public partial class LoginView : UserControl, IDialogControl
{
    private LoginViewModel? ViewModel => DataContext as LoginViewModel;

    public LoginView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _ = ViewModel?.Initialize();
    }
}
