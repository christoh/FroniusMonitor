using De.Hochstaetter.HomeAutomationClient.Extensions;

namespace De.Hochstaetter.HomeAutomationClient.Views;

public partial class SmartMeterDetailsView : ContentPage
{
    public SmartMeterDetailsViewModel ViewModel => (SmartMeterDetailsViewModel)DataContext!;

    public SmartMeterDetailsView()
    {
        InitializeComponent();

        this.UseViewModel<SmartMeterDetailsViewModel>();

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
    /// The gauge background comes from the InverterBackgroundColor converter, which resolves the theme brushes when
    /// it runs. Re-notifying its source property is what makes the gauges repaint after a theme change.
    /// </summary>
    private void OnThemeChanged(object? sender, EventArgs e)
    {
        ViewModel.UpdateService.MeterStatus?.NotifyOfPropertyChange(nameof(Gen24Status.StatusCode));
    }
}
