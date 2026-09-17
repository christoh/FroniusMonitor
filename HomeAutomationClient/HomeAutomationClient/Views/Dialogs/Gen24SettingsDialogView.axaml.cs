namespace De.Hochstaetter.HomeAutomationClient.Views.Dialogs;

public partial class Gen24SettingsDialogView : UserControl, IDialogControl
{
    private Gen24SettingsDialogViewModel? ViewModel => DataContext as Gen24SettingsDialogViewModel;

    public Gen24SettingsDialogView() => InitializeComponent();

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // Initialize reads the settings snapshot. It reports its own failures, so there is nobody to await it.

        if (ViewModel is { } viewModel)
        {
            ViewModelBase.HandleTaskExceptions(viewModel.Initialize);
        }
    }
}
