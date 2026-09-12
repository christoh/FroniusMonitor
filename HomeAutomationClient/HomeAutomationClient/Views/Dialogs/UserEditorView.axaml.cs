namespace De.Hochstaetter.HomeAutomationClient.Views.Dialogs;

public partial class UserEditorView : UserControl, IDialogControl
{
    private UserEditorViewModel? ViewModel => DataContext as UserEditorViewModel;

    public UserEditorView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _ = ViewModel?.Initialize();
    }
}