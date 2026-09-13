namespace De.Hochstaetter.HomeAutomationClient.Views.Dialogs;

public partial class ChangePasswordView : UserControl, IDialogControl
{
    private ChangePasswordViewModel? ViewModel => DataContext as ChangePasswordViewModel;

    public ChangePasswordView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _ = ViewModel?.Initialize();
    }
}
