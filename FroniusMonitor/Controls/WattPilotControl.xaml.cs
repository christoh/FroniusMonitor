using System.Reflection;

namespace De.Hochstaetter.FroniusMonitor.Controls;

public enum WattPilotDisplayMode : byte
{
    Voltage,
    VoltageGauge,
    Current,
    CurrentGauge,
    Power,
    PowerGauge,
    PowerFactor,
    EnergyCards,
    MoreFrequency,
    NeutralWire,
    MoreWifi,
    MoreTemperatures,
}

public partial class WattPilotControl
{
    private static readonly IReadOnlyList<WattPilotDisplayMode> powerModes =
    [
        WattPilotDisplayMode.PowerGauge,
        WattPilotDisplayMode.Power,
        WattPilotDisplayMode.PowerFactor,
    ];

    private static readonly IReadOnlyList<WattPilotDisplayMode> voltageModes =
    [
        WattPilotDisplayMode.VoltageGauge,
        WattPilotDisplayMode.Voltage,
    ];

    private static readonly IReadOnlyList<WattPilotDisplayMode> currentModes =
    [
        WattPilotDisplayMode.CurrentGauge,
        WattPilotDisplayMode.Current,
    ];

    private static readonly IReadOnlyList<WattPilotDisplayMode> moreModes =
    [
        WattPilotDisplayMode.MoreFrequency,
        WattPilotDisplayMode.NeutralWire,
        WattPilotDisplayMode.MoreTemperatures,
        WattPilotDisplayMode.EnergyCards,
        WattPilotDisplayMode.MoreWifi,
    ];

    private int powerIndex, voltageIndex, moreIndex, currentIndex;

    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register
    (
        nameof(Mode), typeof(WattPilotDisplayMode), typeof(WattPilotControl),
        new PropertyMetadata(WattPilotDisplayMode.PowerGauge, (d, _) => ((WattPilotControl)d).OnModeChanged())
    );

    public WattPilotDisplayMode Mode
    {
        get => (WattPilotDisplayMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public static readonly DependencyProperty WattPilotServiceProperty = DependencyProperty.Register
    (
        nameof(WattPilotService), typeof(IWattPilotService), typeof(WattPilotControl)
    );

    public IWattPilotService WattPilotService
    {
        get => (IWattPilotService)GetValue(WattPilotServiceProperty);
        set => SetValue(WattPilotServiceProperty, value);
    }


    public static readonly DependencyProperty HomeAutomationSystemProperty = DependencyProperty.Register
    (
        nameof(HomeAutomationSystem), typeof(HomeAutomationSystem), typeof(WattPilotControl)
    );

    public HomeAutomationSystem HomeAutomationSystem
    {
        get => (HomeAutomationSystem)GetValue(HomeAutomationSystemProperty);
        set => SetValue(HomeAutomationSystemProperty, value);
    }

    public WattPilotControl()
    {
        InitializeComponent();
    }

    private void CycleMode(IReadOnlyList<WattPilotDisplayMode> modeList, ref int index)
    {
        index = modeList.Contains(Mode) ? ++index % modeList.Count : 0;
        Mode = modeList[index];
    }

    private void OnPowerClicked(object sender, RoutedEventArgs e) => CycleMode(powerModes, ref powerIndex);
    private void OnVoltageClicked(object sender, RoutedEventArgs e) => CycleMode(voltageModes, ref voltageIndex);
    private void OnCurrentClicked(object sender, RoutedEventArgs e) => CycleMode(currentModes, ref currentIndex);
    private void OnMoreClicked(object sender, RoutedEventArgs e) => CycleMode(moreModes, ref moreIndex);

    private void OnModeChanged()
    {
        Enum.GetNames<WattPilotDisplayMode>().Apply(enumName =>
        {
            if (GetType().GetField(enumName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)?.GetValue(this) is FrameworkElement element)
            {
                element.Visibility = Mode.ToString() == enumName ? Visibility.Visible : Visibility.Collapsed;
            }
        });
    }

    private void OnSettingsClicked(object sender, RoutedEventArgs e)
    {
        IoC.TryGetRegistered<MainWindow>()?.GetView<WattPilotSettingsView>().Focus();
    }

    private void OnDetailsClicked(object sender, RoutedEventArgs e)
    {
        IoC.TryGetRegistered<MainWindow>()?.GetView<WattPilotDetailsView>().Focus();
    }

    private async void OnRebootWattPilotClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            await WattPilotService.RebootWattPilot().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            var window = Window.GetWindow(this);

            if (window != null)
            {
                MessageBox.Show(window, ex.Message, Loc.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show(ex.Message, Loc.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OpenChargingLogClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            WattPilotService.OpenChargingLog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(Window.GetWindow(this)!, ex.Message, Loc.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenConfigPdfClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            WattPilotService.OpenConfigPdf();
        }
        catch (Exception ex)
        {
            MessageBox.Show(Window.GetWindow(this)!, ex.Message, Loc.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
