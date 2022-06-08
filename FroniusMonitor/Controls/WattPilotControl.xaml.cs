namespace De.Hochstaetter.FroniusMonitor.Controls;

public enum WattPilotDisplayMode : byte
{
    Voltage,
    Current,
    Power,
    PowerFactor,
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

    private int currentAcIndex;

    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register
    (
        nameof(Mode), typeof(WattPilotDisplayMode), typeof(WattPilotControl),
        new PropertyMetadata(WattPilotDisplayMode.Current, (d, _) => ((WattPilotControl)d).OnModeChanged())
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

    private void OnAcClicked(object sender, RoutedEventArgs e) => CycleMode(acModes, ref currentAcIndex);

    private void OnModeChanged()
    {
        LcdCurrent.Visibility = LcdVoltage.Visibility = LcdPowerFactor.Visibility = LcdPower.Visibility = Visibility.Collapsed;

        var lcd = Mode switch
        {
            WattPilotDisplayMode.Current => LcdCurrent,
            WattPilotDisplayMode.Voltage => LcdVoltage,
            WattPilotDisplayMode.Power => LcdPower,
            WattPilotDisplayMode.PowerFactor => LcdPowerFactor,
            _ => throw new NotSupportedException("Unsupported DisplayMode")
        };

        lcd.Visibility = Visibility.Visible;
    }



}