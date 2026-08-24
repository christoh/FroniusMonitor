using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.HomeAutomationClient.Extensions;

namespace De.Hochstaetter.HomeAutomationClient.Views;

public partial class DashboardView : ContentPage
{
    private readonly DashboardViewModel viewModel;

    public DashboardView()
    {
        InitializeComponent();
        DataContext = viewModel = IoC.GetRegistered<DashboardViewModel>();
        _ = viewModel.Initialize();

        Loaded += (_, _) =>
        {
            viewModel.UpdateService.SitePowerFlowUpdated += OnSitePowerFlowUpdated;
            Application.Current!.ActualThemeVariantChanged += OnThemeChanged;
            UpdatePowerFlowColors();
        };

        Unloaded += (_, _) =>
        {
            viewModel.UpdateService.SitePowerFlowUpdated -= OnSitePowerFlowUpdated;
            Application.Current!.ActualThemeVariantChanged -= OnThemeChanged;
        };
    }

    private void OnSitePowerFlowUpdated(object? sender, SitePowerFlowUpdatedEventArgs e) => _ = Dispatcher.UIThread.InvokeAsync(UpdatePowerFlowColors);

    private void OnThemeChanged(object? sender, EventArgs e) => UpdatePowerFlowColors();

    private void UpdatePowerFlowColors() => viewModel.UpdatePowerFlowColors
    (
        GetThemeColor("PowerFlowGrid"),
        GetThemeColor("PowerFlowSolar"),
        GetThemeColor("PowerFlowBattery")
    );

    private static HaColor GetThemeColor(string key) => Application.Current!.GetSolidColorBrush(key)!.Color.ToUInt32();
}
