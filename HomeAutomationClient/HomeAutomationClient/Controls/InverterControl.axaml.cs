using Avalonia.Controls.Primitives;
using Avalonia.Media.Immutable;
using De.Hochstaetter.HomeAutomationClient.Extensions;

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


public partial class InverterControl : DeviceControlBase
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

    private static readonly IWebClientService webClient = IoC.Get<IWebClientService>();

    private int currentAcIndex, currentDcIndex, currentMoreIndex, energyIndex;

    public static readonly StyledProperty<Gen24System?> InverterProperty = AvaloniaProperty.Register<InverterControl, Gen24System?>(nameof(Inverter));

    public Gen24System? Inverter
    {
        get => GetValue(InverterProperty);
        set => SetValue(InverterProperty, value);
    }

    public static readonly DirectProperty<InverterControl, IBrush?> AcProducedPowerBrushProperty = AvaloniaProperty.RegisterDirect<InverterControl, IBrush?>(nameof(AcProducedPowerBrush), o => o.AcProducedPowerBrush);

    public IBrush? AcProducedPowerBrush
    {
        get;
        set => SetAndRaise(AcProducedPowerBrushProperty, ref field, value);
    }

    public static readonly StyledProperty<IBrush> LoadPowerBrushProperty = AvaloniaProperty.Register<InverterControl, IBrush>(nameof(LoadPowerBrush), new ImmutableSolidColorBrush(Color.FromUInt32(0xff807000)));

    public IBrush LoadPowerBrush
    {
        get => GetValue(LoadPowerBrushProperty);
        set => SetValue(LoadPowerBrushProperty, value);
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

        AcProducedPowerBrush = Application.Current!.TryGetResource("PowerFlowSolar", Application.Current.ActualThemeVariant, out var value) && value is IBrush brush
            ? brush
            : new ImmutableSolidColorBrush(Color.FromUInt32(0xff807000));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        switch (e.Property.Name)
        {
            case nameof(Inverter):
                if (e.OldValue is INotifyPropertyChanged oldDevice)
                {
                    oldDevice.PropertyChanged -= OnInverterPropertyChanged;
                }

                if (e.NewValue is INotifyPropertyChanged newDevice)
                {
                    newDevice.PropertyChanged += OnInverterPropertyChanged;
                    SetAcPowerBrushColor();
                    OnInverterPropertyChanged(Inverter, new PropertyChangedEventArgs(string.Empty));
                }

                break;
            
            case nameof(LoadPowerBrush):
                if ((Inverter?.Sensors?.Inverter?.StoragePower ?? 0)+(Inverter?.Sensors?.Inverter?.SolarPowerSum ?? 0) < 0)
                {
                   AcProducedPowerBrush = LoadPowerBrush;
                }

                break;
        }
    }

    private void OnInverterPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == string.Empty)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                ChangeOuter();
                ChangeInner();
            });
        }
    }

    protected override void ChangeOuter()
    {
        BackgroundProvider.Background = Inverter?.Sensors?.InverterStatus?.StatusCode switch
        {
            "STATE_ERROR" => OuterFault,
            "STATE_RUNNING" => OuterRunning,
            "STATE_WARNING" => OuterWarning,
            "STATE_STARTUP" => OuterStartup,
            _ => OuterOther,
        };
    }

    protected override void ChangeInner()
    {
        InnerBackgroundProvider.Background = Inverter?.Sensors?.InverterStatus?.StatusCode switch
        {
            "STATE_ERROR" => InnerFault,
            "STATE_RUNNING" => InnerRunning,
            "STATE_WARNING" => InnerWarning,
            "STATE_STARTUP" => InnerStartup,
            _ => InnerOther,
        };
    }

    private void SetAcPowerBrushColor()
    {
        var storagePower = Inverter?.Sensors?.Inverter?.StoragePower ?? 0;
        var solarPower = Inverter?.Sensors?.Inverter?.SolarPowerSum ?? 0;
        var powerSum = storagePower + solarPower;
        ISolidColorBrush gridPowerBrush = Application.Current!.GetSolidColorBrush("PowerFlowGrid")!;

        if (Inverter?.Sensors?.InverterStatus?.Status != 1 || powerSum == 0)
        {
            AcProducedPowerBrush = new ImmutableSolidColorBrush(gridPowerBrush);
            return;
        }

        if (powerSum > 0)
        {
            ISolidColorBrush solarPowerBrush = Application.Current!.GetSolidColorBrush("PowerFlowSolar")!;
            ISolidColorBrush storagePowerBrush = Application.Current!.GetSolidColorBrush("PowerFlowBattery")!;
            var solarWeight = (float)(solarPower / powerSum);
            AcProducedPowerBrush = new ImmutableSolidColorBrush(storagePowerBrush.Color.MixWith(solarPowerBrush.Color, solarWeight));
        }
        
        return;

        static byte Round(double value)
        {
            return (byte)Math.Round(value, MidpointRounding.ToZero);
        }
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

    private async void OnStandbyClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not ToggleButton { IsChecked: not null } button || DeviceKey is not string deviceKey)
            {
                return;
            }

            var result = await webClient.RequestGen24StandBy(deviceKey, !button.IsChecked.Value);

            if (result.Status != HttpStatusCode.OK)
            {
                button.IsChecked = !button.IsChecked.Value;
                await ViewModelBase.ShowHttpError(result);
            }
        }
        catch
        {
            // async void must be caught
        }
    }
}