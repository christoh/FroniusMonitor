namespace FroniusPhone.Pages;

public partial class AppShell
{

    public static BindableProperty NeedInitialSettingsProperty = BindableProperty.Create(nameof(NeedInitialSettings), typeof(bool), typeof(AppShell), false);

    public bool NeedInitialSettings
    {
        get => (bool)GetValue(NeedInitialSettingsProperty);
        set => SetValue(NeedInitialSettingsProperty, value);
    }

    public AppShell(SettingsViewModel viewModel)
    {
            InitializeComponent();
            BindingContext = ViewModel = viewModel;
        }

    public SettingsViewModel ViewModel { get; }
}