namespace De.Hochstaetter.HomeAutomationClient.Views;

public partial class BatteryDetailsView : ContentPage
{
    public BatteryDetailsViewModel ViewModel => (BatteryDetailsViewModel)DataContext!;

    public BatteryDetailsView()
    {
        InitializeComponent();

        // Parameterless, so the XAML runtime loader and the previewer can create the view (AVLN3001). The container
        // hands out the same singleton view model it would have injected; Try..., because the designer has no container.
        DataContext = IoC.TryGetRegistered<BatteryDetailsViewModel>();

        Loaded += (_, _) =>
        {
            Application.Current!.ActualThemeVariantChanged += OnThemeChanged;
        };

        Unloaded += (_, _) =>
        {
            Application.Current!.ActualThemeVariantChanged -= OnThemeChanged;
        };
    }

    /// <summary>
    /// The gauge background comes from the DeviceBackgroundColor converter, which resolves the theme brushes when it
    /// runs. Re-notifying its source property is what makes the gauges repaint after a theme change.
    /// </summary>
    private void OnThemeChanged(object? sender, EventArgs e)
    {
        ViewModel.Gen24System.Sensors?.Storage?.NotifyOfPropertyChange(nameof(Gen24Storage.IsAwake));
    }
}
