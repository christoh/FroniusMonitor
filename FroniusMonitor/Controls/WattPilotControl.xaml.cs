using System.Reflection;

namespace De.Hochstaetter.FroniusMonitor.Controls;

public enum WattPilotDisplayMode : byte
{
    Voltage,
    Current,
    CurrentGauge,
    Power,
    PowerGauge,
    PowerFactor,
    EnergyCards,
    MoreFrequency,
    NeutralWire,
    MoreWifi,
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
        WattPilotDisplayMode.EnergyCards,
        WattPilotDisplayMode.MoreWifi,
    ];

    public static IReadOnlyList<MultiColorGauge.ColorEntry> LowIsBad { get; } =
    [
        new MultiColorGauge.ColorEntry(0,Colors.Red),
        new MultiColorGauge.ColorEntry(.05,Colors.OrangeRed),
        new MultiColorGauge.ColorEntry(.5,Colors.YellowGreen),
        new MultiColorGauge.ColorEntry(1,Colors.Green),
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
        nameof(WattPilotService), typeof(IWattPilotService), typeof(WattPilotControl),
        new PropertyMetadata((d, _) => ((WattPilotControl)d).OnWattPilotServiceChanged())
    );

    public IWattPilotService WattPilotService
    {
        get => (IWattPilotService)GetValue(WattPilotServiceProperty);
        set => SetValue(WattPilotServiceProperty, value);
    }

    private void OnWattPilotServiceChanged() { }

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
        IoC.GetRegistered<MainWindow>().GetView<WattPilotSettingsView>().Focus();
    }
}
