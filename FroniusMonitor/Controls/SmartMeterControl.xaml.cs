namespace De.Hochstaetter.FroniusMonitor.Controls;

public enum MeterDisplayMode
{
    PowerReal,
    PowerApparent,
    PowerReactive,
    PowerFactor,
    PowerOutOfBalance,
    PhaseVoltage,
    LineVoltage,
    Current,
    CurrentOutOfBalance,
    More,
    MoreEnergy,
}

public partial class SmartMeterControl : IHaveLcdPanel
{
    private static readonly IReadOnlyList<MeterDisplayMode> powerModes = new[]
    {
        MeterDisplayMode.PowerReal, MeterDisplayMode.PowerApparent,
        MeterDisplayMode.PowerReactive, MeterDisplayMode.PowerFactor,
        MeterDisplayMode.PowerOutOfBalance
    };

    private static readonly IReadOnlyList<MeterDisplayMode> voltageModes = new[]
    {
        MeterDisplayMode.PhaseVoltage, MeterDisplayMode.LineVoltage
    };

    private static readonly IReadOnlyList<MeterDisplayMode> currentModes = new[]
    {
        MeterDisplayMode.Current, MeterDisplayMode.CurrentOutOfBalance
    };

    private static readonly IReadOnlyList<MeterDisplayMode> moreModes = new[]
    {
        MeterDisplayMode.MoreEnergy, MeterDisplayMode.More
    };

    private int currentPowerModeIndex, currentVoltageModeIndex, currentCurrentIndex, currentMoreIndex;
    private readonly IDataCollectionService? solarSystemService = IoC.TryGetRegistered<IDataCollectionService>();

    #region Dependency Properties

    public static readonly DependencyProperty SmartMeterProperty = DependencyProperty.Register
    (
        nameof(SmartMeter), typeof(Gen24PowerMeter3P), typeof(SmartMeterControl)
    );

    public Gen24PowerMeter3P? SmartMeter
    {
        get => (Gen24PowerMeter3P?)GetValue(SmartMeterProperty);
        set => SetValue(SmartMeterProperty, value);
    }

    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register
    (
        nameof(Mode), typeof(MeterDisplayMode), typeof(SmartMeterControl),
        new PropertyMetadata(MeterDisplayMode.PowerReal, (d, _) => ((SmartMeterControl)d).SmartMeterDataChanged())
    );

    public MeterDisplayMode Mode
    {
        get => (MeterDisplayMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    #endregion

    public SmartMeterControl()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            if (solarSystemService != null)
            {
                solarSystemService.NewDataReceived += NewDataReceived; 
            }
        };

        Unloaded += (_, _) =>
        {
            if (solarSystemService != null)
            {
                solarSystemService.NewDataReceived -= NewDataReceived;
            }
        };

    }

    private void NewDataReceived(object? sender, SolarDataEventArgs e)
    {
        SmartMeterDataChanged();
    }

    private void SmartMeterDataChanged() => Dispatcher.InvokeAsync(() =>
    {
        if (SmartMeter is null)
        {
            BackgroundProvider.Background = Brushes.LightGray;
            Title.Text = "---";
            return;
        }

        Title.Text = $"{SmartMeter.Model} ({solarSystemService?.HomeAutomationSystem?.Gen24Sensors?.MeterStatus?.StatusMessage??Loc.Unknown})";
        BackgroundProvider.Background = solarSystemService?.HomeAutomationSystem?.Gen24Sensors?.MeterStatus?.ToBrush() ?? Brushes.LightGray;

        switch (Mode)
        {
            case MeterDisplayMode.PowerReal:
                Lcd.Header = Loc.RealPower;
                Lcd.Value1 = $"{SmartMeter.RealPowerL1:N1} W";
                Lcd.Value2 = $"{SmartMeter.RealPowerL2:N1} W";
                Lcd.Value3 = $"{SmartMeter.RealPowerL3:N1} W";
                Lcd.ValueSum = $"{SmartMeter.RealPowerSum:N1} W";
                SetL123("Sum");
                break;

            case MeterDisplayMode.PowerApparent:
                Lcd.Header = Loc.ApparentPower;
                Lcd.Value1 = $"{SmartMeter.ApparentPowerL1:N1} W";
                Lcd.Value2 = $"{SmartMeter.ApparentPowerL2:N1} W";
                Lcd.Value3 = $"{SmartMeter.ApparentPowerL3:N1} W";
                Lcd.ValueSum = $"{SmartMeter.ApparentPowerSum:N1} W";
                SetL123("Sum");
                break;

            case MeterDisplayMode.PowerReactive:
                Lcd.Header = Loc.ReactivePower;
                Lcd.Value1 = $"{SmartMeter.ReactivePowerL1:N1} W";
                Lcd.Value2 = $"{SmartMeter.ReactivePowerL2:N1} W";
                Lcd.Value3 = $"{SmartMeter.ReactivePowerL3:N1} W";
                Lcd.ValueSum = $"{SmartMeter.ReactivePowerSum:N1} W";
                SetL123("Sum");
                break;

            case MeterDisplayMode.PowerFactor:
                Lcd.Header = Loc.PowerFactor;
                Lcd.Value1 = $"{SmartMeter.PowerFactorL1:N3}";
                Lcd.Value2 = $"{SmartMeter.PowerFactorL2:N3}";
                Lcd.Value3 = $"{SmartMeter.PowerFactorL3:N3}";
                Lcd.ValueSum = $"{SmartMeter.PowerFactorTotal:N3}";
                SetL123("Tot");
                break;

            case MeterDisplayMode.PhaseVoltage:
                Lcd.Header = Loc.PhaseVoltage;
                Lcd.Value1 = $"{SmartMeter.PhaseVoltageL1:N1} V";
                Lcd.Value2 = $"{SmartMeter.PhaseVoltageL2:N1} V";
                Lcd.Value3 = $"{SmartMeter.PhaseVoltageL3:N1} V";
                Lcd.ValueSum = $"{SmartMeter.PhaseVoltageAverage:N1} V";
                SetL123("Avg");
                break;

            case MeterDisplayMode.LineVoltage:
                Lcd.Header = Loc.LineVoltage;
                Lcd.Value1 = $"{SmartMeter.LineVoltageL12:N1} V";
                Lcd.Value2 = $"{SmartMeter.LineVoltageL23:N1} V";
                Lcd.Value3 = $"{SmartMeter.LineVoltageL31:N1} V";
                Lcd.ValueSum = $"{SmartMeter.LineVoltageAverage:N1} V";
                SetTwoPhases("Avg");
                break;

            case MeterDisplayMode.Current:
                Lcd.Header = Loc.Current;
                Lcd.Value1 = $"{SmartMeter.CurrentL1:N3} A";
                Lcd.Value2 = $"{SmartMeter.CurrentL2:N3} A";
                Lcd.Value3 = $"{SmartMeter.CurrentL3:N3} A";
                Lcd.ValueSum = $"{SmartMeter.TotalCurrent:N3} A";
                SetL123("Sum");
                break;

            case MeterDisplayMode.PowerOutOfBalance:
                Lcd.Header = Loc.OutOfBalance;
                Lcd.Value1 = $"{SmartMeter.OutOfBalancePowerL12:N1} W";
                Lcd.Value2 = $"{SmartMeter.OutOfBalancePowerL23:N1} W";
                Lcd.Value3 = $"{SmartMeter.OutOfBalancePowerL31:N1} W";
                Lcd.ValueSum = $"{SmartMeter.OutOfBalancePowerMax:N1} W";
                SetTwoPhases("Max");
                break;

            case MeterDisplayMode.More:
                Lcd.Header = SmartMeter.SerialNumber;
                Lcd.Value1 = $"{SmartMeter.Frequency:N1} Hz";
                Lcd.Label1 = "Frq";
                Lcd.Value2 = $"{SmartMeter.DataTime?.ToLocalTime():d}";
                Lcd.Label2 = "Dat";
                Lcd.Value3 = $"{SmartMeter.DataTime?.ToLocalTime():T}";
                Lcd.Label3 = "Tim";
                Lcd.ValueSum = $"{SmartMeter.IsVisible}";
                Lcd.LabelSum = "Val";
                break;

            case MeterDisplayMode.MoreEnergy:
                Lcd.Header = $"{Loc.Energy} (kWh)";
                Lcd.Value1 = $"{SmartMeter.EnergyRealConsumed / 1000:N1}";
                Lcd.Label1 = "CRl";
                Lcd.Value2 = $"{SmartMeter.EnergyRealProduced / 1000:N1}";
                Lcd.Label2 = "PRl";
                Lcd.Value3 = $"{SmartMeter.EnergyReactiveConsumed / 1000:N1}";
                Lcd.Label3 = "CRv";
                Lcd.ValueSum = $"{SmartMeter.EnergyReactiveProduced / 1000:N1}";
                Lcd.LabelSum = "PRv";
                break;

            case MeterDisplayMode.CurrentOutOfBalance:
                Lcd.Header = Loc.OutOfBalance;
                Lcd.Value1 = $"{SmartMeter.OutOfBalanceCurrentL12:N3} A";
                Lcd.Value2 = $"{SmartMeter.OutOfBalanceCurrentL23:N3} A";
                Lcd.Value3 = $"{SmartMeter.OutOfBalanceCurrentL31:N3} A";
                Lcd.ValueSum = $"{SmartMeter.OutOfBalanceCurrentMax:N3} A";
                SetTwoPhases("Max");
                break;
        }
    });

    private void SetL123(string sumText) => IHaveLcdPanel.SetL123(Lcd, sumText);
    private void SetTwoPhases(string sumText) => IHaveLcdPanel.SetTwoPhases(Lcd, sumText);

    private void SetMode(IReadOnlyList<MeterDisplayMode> modeList, ref int index)
    {
        index = modeList.Contains(Mode) ? ++index % modeList.Count : 0;
        Mode = modeList[index];
    }

    private void OnPowerClick(object sender, RoutedEventArgs e) => SetMode(powerModes, ref currentPowerModeIndex);

    private void OnVoltageClick(object sender, RoutedEventArgs e) => SetMode(voltageModes, ref currentVoltageModeIndex);

    private void OnCurrentClick(object sender, RoutedEventArgs e) => SetMode(currentModes, ref currentCurrentIndex);

    private void OnMoreClick(object sender, RoutedEventArgs e) => SetMode(moreModes, ref currentMoreIndex);
}
