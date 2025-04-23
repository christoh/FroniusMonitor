using Avalonia.Interactivity;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

public enum InverterDisplayMode
{
    AcPhaseVoltage,
    AcPhaseVoltageGauge,
    AcLineVoltage,
    AcCurrent,
    AcPowerActive,
    AcPowerActiveGauge,
    AcPowerApparent,
    AcPowerReactive,
    AcPowerFactor,
    DcPowerGauge,
    DcVoltage,
    DcCurrent,
    DcPower,
    DcRelativePower,
    MpptComparison,
    EnergyInverter,
    EnergyRectifier,
    EnergyStorage,
    EnergySolar,
    More,
    MoreEfficiency,
    MoreTemperatures,
    MoreFans,
    MoreVersions,
    MoreOp,
}


public partial class InverterControl : UserControl
{
    private static readonly IReadOnlyList<InverterDisplayMode> acModes =
    [
        InverterDisplayMode.AcPowerActiveGauge,
        InverterDisplayMode.AcPowerActive,
        InverterDisplayMode.AcPowerApparent,
        InverterDisplayMode.AcPowerReactive,
        InverterDisplayMode.AcPowerFactor,
        InverterDisplayMode.AcCurrent,
        InverterDisplayMode.AcPhaseVoltageGauge,
        InverterDisplayMode.AcPhaseVoltage,
        InverterDisplayMode.AcLineVoltage,
    ];

    private static readonly IReadOnlyList<InverterDisplayMode> dcModes =
    [
        InverterDisplayMode.DcPowerGauge,
        InverterDisplayMode.DcPower,
        InverterDisplayMode.DcRelativePower,
        InverterDisplayMode.MpptComparison,
        InverterDisplayMode.DcCurrent,
        InverterDisplayMode.DcVoltage,
    ];

    private static readonly IReadOnlyList<InverterDisplayMode> moreModes =
    [
        InverterDisplayMode.MoreEfficiency,
        InverterDisplayMode.More,
        InverterDisplayMode.MoreTemperatures,
        InverterDisplayMode.MoreFans,
        InverterDisplayMode.MoreOp,
        InverterDisplayMode.MoreVersions
    ];

    private static readonly IReadOnlyList<InverterDisplayMode> energyModes =
    [
        InverterDisplayMode.EnergySolar,
        InverterDisplayMode.EnergyInverter,
        InverterDisplayMode.EnergyRectifier,
        InverterDisplayMode.EnergyStorage,
    ];

    private int currentAcIndex, currentDcIndex, currentMoreIndex, energyIndex;

    public static readonly StyledProperty<Gen24System> InverterProperty = AvaloniaProperty.Register<InverterControl, Gen24System>(nameof(Inverter));

    public Gen24System Inverter
    {
        get => GetValue(InverterProperty);
        set => SetValue(InverterProperty, value);
    }
    
    public static readonly StyledProperty<bool> ColorAllTicksProperty = AvaloniaProperty.Register<InverterControl, bool>(nameof(ColorAllTicks));
    
    public bool ColorAllTicks
    {
        get => GetValue(ColorAllTicksProperty);
        set => SetValue(ColorAllTicksProperty, value);
    }

    public static readonly StyledProperty<InverterDisplayMode> ModeProperty = AvaloniaProperty.Register<InverterControl, InverterDisplayMode>(nameof(Mode), InverterDisplayMode.DcPowerGauge);

    public InverterDisplayMode Mode
    {
        get => GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public InverterControl()
    {
        InitializeComponent();
    }
    
    private void CycleMode(IReadOnlyList<InverterDisplayMode> modeList, ref int index)
    {
        index = modeList.Contains(Mode) ? ++index % modeList.Count : 0;
        Mode = modeList[index];
    }

    private void OnAcClicked(object sender, RoutedEventArgs e) => CycleMode(acModes, ref currentAcIndex);

    private void OnDcClicked(object sender, RoutedEventArgs e) => CycleMode(dcModes, ref currentDcIndex);

    private void OnEnergyClicked(object sender, RoutedEventArgs e) => CycleMode(energyModes, ref energyIndex);

    private void OnMoreClicked(object sender, RoutedEventArgs e) => CycleMode(moreModes, ref currentMoreIndex);
    
}