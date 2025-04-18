namespace De.Hochstaetter.HomeAutomationClient.Views.Dialogs;

public partial class LoginView : UserControl, IDialogControl
{
    private LoginViewModel? ViewModel => DataContext as LoginViewModel;

    public LoginView()
    {
        InitializeComponent();
        Loaded += (_, _) => ViewModel?.Initialize();
    }
}
