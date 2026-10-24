using De.Hochstaetter.HomeAutomationClient.Extensions;

namespace De.Hochstaetter.HomeAutomationClient.Views;

public partial class WattPilotDetailsView : ContentPage
{
    public WattPilotDetailsViewModel ViewModel => (WattPilotDetailsViewModel)DataContext!;

    public WattPilotDetailsView()
    {
        InitializeComponent();

        this.UseViewModel<WattPilotDetailsViewModel>();

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
    /// runs. Re-notifying the view model is what makes the gauges repaint after a theme change.
    /// </summary>
    private void OnThemeChanged(object? sender, EventArgs e)
    {
        ViewModel.NotifyOfPropertyChange(nameof(WattPilotDetailsViewModel.WattPilot));
    }
}
