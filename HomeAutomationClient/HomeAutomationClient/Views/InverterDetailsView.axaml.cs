namespace De.Hochstaetter.HomeAutomationClient.Views;

public partial class InverterDetailsView : ContentPage
{
    public static readonly StyledProperty<bool> UseRunningBackgroundProperty = AvaloniaProperty.RegisterAttached<InverterDetailsView, bool>(nameof(GetUseRunningBackground)[3..], typeof(InverterDetailsView));

    public static bool GetUseRunningBackground(AvaloniaObject element)
    {
        return element.GetValue(UseRunningBackgroundProperty);
    }

    public static void SetUseRunningBackground(AvaloniaObject element, bool value)
    {
        element.SetValue(UseRunningBackgroundProperty, value);
    }

    public InverterDetailsViewModel ViewModel =>(InverterDetailsViewModel)DataContext!;

    public InverterDetailsView()
    {
        InitializeComponent();

        // Parameterless, so the XAML runtime loader and the previewer can create the view (AVLN3001). The container
        // hands out the same singleton view model it would have injected; Try..., because the designer has no container.
        DataContext = IoC.TryGetRegistered<InverterDetailsViewModel>();

        Loaded += (_, _) =>
        {
            Application.Current!.ActualThemeVariantChanged += OnThemeChanged; 
        };

        Unloaded += (_, _) =>
        {
            Application.Current!.ActualThemeVariantChanged -= OnThemeChanged;
        };
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        ViewModel.Gen24System.Sensors?.InverterStatus?.NotifyOfPropertyChange(nameof(Gen24Status.StatusCode));
    }
}
