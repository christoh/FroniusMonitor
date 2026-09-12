namespace De.Hochstaetter.HomeAutomationClient.Views.Dialogs;

public partial class UserManagementView : UserControl, IDialogControl
{
    private UserManagementViewModel? ViewModel => DataContext as UserManagementViewModel;

    public UserManagementView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _ = ViewModel?.Initialize();
    }

    private void OnUserDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (ViewModel?.EditCommand.CanExecute(null) == true)
        {
            ViewModel.EditCommand.Execute(null);
        }
    }
}