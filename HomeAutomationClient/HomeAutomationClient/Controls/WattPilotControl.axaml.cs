using De.Hochstaetter.Fronius.Models.Charging;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

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

public partial class WattPilotControl : DeviceControlBase
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

    #region Avalonia Properties

    public static readonly StyledProperty<WattPilotDisplayMode> ModeProperty = AvaloniaProperty.Register<WattPilotControl, WattPilotDisplayMode>(nameof(Mode), WattPilotDisplayMode.PowerGauge);

    public WattPilotDisplayMode Mode
    {
        get => GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public static readonly StyledProperty<WattPilot> WattPilotProperty = AvaloniaProperty.Register<WattPilotControl, WattPilot>(nameof(WattPilot));

    public WattPilot WattPilot
    {
        get => GetValue(WattPilotProperty);
        set => SetValue(WattPilotProperty, value);
    }

    public static readonly StyledProperty<bool> ColorAllTicksProperty = AvaloniaProperty.Register<SmartMeterControl, bool>(nameof(ColorAllTicks));

    public bool ColorAllTicks
    {
        get => GetValue(ColorAllTicksProperty);
        set => SetValue(ColorAllTicksProperty, value);
    }

    #endregion

    public WattPilotControl()
    {
        InitializeComponent();
    }

    protected override void ChangeInner()
    {
        InnerBackgroundProvider.Background = InnerRunning;
    }

    protected override void ChangeOuter()
    {
        BackgroundProvider.Background = OuterRunning;
    }

    private void SetMode(IReadOnlyList<WattPilotDisplayMode> modeList, ref int index)
    {
        index = modeList.Contains(Mode) ? ++index % modeList.Count : 0;
        Mode = modeList[index];
    }

    private void OnPowerClick(object sender, RoutedEventArgs e) => SetMode(powerModes, ref powerIndex);

    private void OnVoltageClick(object sender, RoutedEventArgs e) => SetMode(voltageModes, ref voltageIndex);

    private void OnCurrentClick(object sender, RoutedEventArgs e) => SetMode(currentModes, ref currentIndex);

    private void OnMoreClick(object sender, RoutedEventArgs e) => SetMode(moreModes, ref moreIndex);
}
