namespace De.Hochstaetter.HomeAutomationClient.Views.Dialogs;

public partial class UserManagementView : DialogView
{
    private UserManagementViewModel? ViewModel => DataContext as UserManagementViewModel;

    public UserManagementView()
    {
        InitializeComponent();
    }

    private void OnUserDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (ViewModel?.EditCommand.CanExecute(null) == true)
        {
            ViewModel.EditCommand.Execute(null);
        }
    }
}