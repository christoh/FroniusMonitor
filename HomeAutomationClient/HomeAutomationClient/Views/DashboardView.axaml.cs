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
        ViewModelBase.HandleTaskExceptions(viewModel.Initialize);
        ViewVisibility.Follow(this);

        Loaded += (_, _) =>
        {
            viewModel.UpdateService.SitePowerFlowUpdated += OnSitePowerFlowUpdated;
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            Application.Current!.ActualThemeVariantChanged += OnThemeChanged;
            UpdatePowerFlowColors();
        };

        Unloaded += (_, _) =>
        {
            viewModel.UpdateService.SitePowerFlowUpdated -= OnSitePowerFlowUpdated;
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            Application.Current!.ActualThemeVariantChanged -= OnThemeChanged;
        };
    }

    /// <summary>
    /// The colors follow every report of the inverters, and nobody sees them while the dashboard cannot be seen;
    /// the report that comes in then is left alone, and the colors are worked out once when it is back.
    /// </summary>
    private void OnSitePowerFlowUpdated(object? sender, SitePowerFlowUpdatedEventArgs e)
    {
        if (viewModel.IsShown)
        {
            _ = Dispatcher.UIThread.InvokeAsync(UpdatePowerFlowColors);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DashboardViewModel.IsShown) && viewModel.IsShown)
        {
            UpdatePowerFlowColors();
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e) => UpdatePowerFlowColors();

    private void UpdatePowerFlowColors() => viewModel.UpdatePowerFlowColors
    (
        GetThemeColor("PowerFlowGrid"),
        GetThemeColor("PowerFlowSolar"),
        GetThemeColor("PowerFlowBattery")
    );

    private static HaColor GetThemeColor(string key) => Application.Current!.GetSolidColorBrush(key)!.Color.ToUInt32();
}
