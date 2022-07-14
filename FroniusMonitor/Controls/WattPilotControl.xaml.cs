namespace De.Hochstaetter.FroniusMonitor.Controls;

public enum WattPilotDisplayMode : byte
{
    Voltage,
    Current,
    Power,
    PowerFactor,
    EnergyCards,
    MoreFrequency
}

public partial class WattPilotControl
{
    private static readonly IReadOnlyList<WattPilotDisplayMode> powerModes = new[]
    {
        WattPilotDisplayMode.Power,
        WattPilotDisplayMode.PowerFactor,
    };

    private static readonly IReadOnlyList<WattPilotDisplayMode> voltageModes = new[]
    {
        WattPilotDisplayMode.Voltage,
    };

    private static readonly IReadOnlyList<WattPilotDisplayMode> currentModes = new[]
    {
        WattPilotDisplayMode.Current,
    };

    private static readonly IReadOnlyList<WattPilotDisplayMode> moreModes = new[]
    {
        WattPilotDisplayMode.MoreFrequency,
        WattPilotDisplayMode.EnergyCards,
    };

    private int powerIndex, voltageIndex, moreIndex, currentIndex;

    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register
    (
        nameof(Mode), typeof(WattPilotDisplayMode), typeof(WattPilotControl),
        new PropertyMetadata(WattPilotDisplayMode.Power, (d, _) => ((WattPilotControl)d).OnModeChanged())
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
        LcdMoreFrequency.Visibility = LcdEnergyCards.Visibility = LcdCurrent.Visibility = LcdVoltage.Visibility = LcdPowerFactor.Visibility = LcdPower.Visibility = Visibility.Collapsed;

        FrameworkElement lcd = Mode switch
        {
            WattPilotDisplayMode.Current => LcdCurrent,
            WattPilotDisplayMode.Voltage => LcdVoltage,
            WattPilotDisplayMode.Power => LcdPower,
            WattPilotDisplayMode.PowerFactor => LcdPowerFactor,
            WattPilotDisplayMode.EnergyCards => LcdEnergyCards,
            WattPilotDisplayMode.MoreFrequency=>LcdMoreFrequency,
            _ => throw new NotSupportedException("Unsupported DisplayMode")
        };

        lcd.Visibility = Visibility.Visible;
    }
}