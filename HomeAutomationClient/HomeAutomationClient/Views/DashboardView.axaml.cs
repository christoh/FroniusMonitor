using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.HomeAutomationClient.Extensions;

namespace De.Hochstaetter.HomeAutomationClient.Views;

public partial class DashboardView : ContentPage
{
    /// <summary>Null in the designer, which has no container; the view then follows nothing.</summary>
    private readonly DashboardViewModel? viewModel;

    public DashboardView()
    {
        InitializeComponent();
        viewModel = this.UseViewModel<DashboardViewModel>();
        ViewVisibility.Follow(this);

        if (viewModel is not { } model)
        {
            return;
        }

        Loaded += (_, _) =>
        {
            model.UpdateService.SitePowerFlowUpdated += OnSitePowerFlowUpdated;
            model.PropertyChanged += OnViewModelPropertyChanged;
            Application.Current!.ActualThemeVariantChanged += OnThemeChanged;
            UpdatePowerFlowColors();
        };

        Unloaded += (_, _) =>
        {
            model.UpdateService.SitePowerFlowUpdated -= OnSitePowerFlowUpdated;
            model.PropertyChanged -= OnViewModelPropertyChanged;
            Application.Current!.ActualThemeVariantChanged -= OnThemeChanged;
        };
    }

    /// <summary>
    /// The colors follow every report of the inverters, and nobody sees them while the dashboard cannot be seen;
    /// the report that comes in then is left alone, and the colors are worked out once when it is back.
    /// </summary>
    private void OnSitePowerFlowUpdated(object? sender, SitePowerFlowUpdatedEventArgs e)
    {
        if (viewModel is { IsShown: true })
        {
            _ = Dispatcher.UIThread.InvokeAsync(UpdatePowerFlowColors);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DashboardViewModel.IsShown) && viewModel is { IsShown: true })
        {
            UpdatePowerFlowColors();
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e) => UpdatePowerFlowColors();

    private void UpdatePowerFlowColors() => viewModel?.UpdatePowerFlowColors
    (
        GetThemeColor("PowerFlowGrid"),
        GetThemeColor("PowerFlowSolar"),
        GetThemeColor("PowerFlowBattery")
    );

    private static HaColor GetThemeColor(string key) => Application.Current!.GetSolidColorBrush(key)!.Color.ToUInt32();
}
