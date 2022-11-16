namespace FroniusPhone.Pages;

public partial class SettingsPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = ViewModel = viewModel;
        HandlerChanged += (s, e) => ViewModel.Dispatcher = Dispatcher;

        NavigatedFrom += async (_, _) =>
        {
            await ViewModel.Settings.SaveAsync().ConfigureAwait(false);
        };
    }

    public SettingsViewModel ViewModel { get; }
}