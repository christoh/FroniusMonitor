using De.Hochstaetter.HomeAutomationClient.Extensions;

namespace De.Hochstaetter.HomeAutomationClient.Views;

public partial class BatteryDetailsView : ContentPage
{
    public BatteryDetailsViewModel ViewModel => (BatteryDetailsViewModel)DataContext!;

    public BatteryDetailsView()
    {
        InitializeComponent();

        this.UseViewModel<BatteryDetailsViewModel>();

        this.FollowApplicationTheme(OnThemeChanged);
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
