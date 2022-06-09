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
    private static readonly IReadOnlyList<WattPilotDisplayMode> acModes = new[]
    {
        WattPilotDisplayMode.Power,
        WattPilotDisplayMode.Current,
        WattPilotDisplayMode.Voltage,
        WattPilotDisplayMode.PowerFactor,
    };

    private static readonly IReadOnlyList<WattPilotDisplayMode> energyModes = new[]
    {
        WattPilotDisplayMode.EnergyCards,
    };

    private static readonly IReadOnlyList<WattPilotDisplayMode> moreModes = new[]
    {
        WattPilotDisplayMode.MoreFrequency,
    };

    private int currentAcIndex, currentEnergyIndex, currentMoreIndex;

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

    private void OnEnergyClicked(object sender, RoutedEventArgs e) => CycleMode(energyModes, ref currentEnergyIndex);
    private void OnAcClicked(object sender, RoutedEventArgs e) => CycleMode(acModes, ref currentAcIndex);
    private void OnMoreClicked(object sender, RoutedEventArgs e) => CycleMode(moreModes, ref currentMoreIndex);

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