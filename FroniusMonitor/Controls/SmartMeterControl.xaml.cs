using System.Reflection;

namespace De.Hochstaetter.FroniusMonitor.Controls;

public enum MeterDisplayMode
{
    PowerActiveGauge,
    PowerActive,
    PowerApparent,
    PowerReactive,
    PowerFactor,
    PowerOutOfBalance,
    PhaseVoltage,
    PhaseVoltageGauge,
    LineVoltage,
    Current,
    CurrentOutOfBalance,
    CurrentOutOfBalanceGauge,
    More,
    MoreEnergy,
}

public partial class SmartMeterControl
{
    private static readonly IReadOnlyList<MeterDisplayMode> powerModes = new[]
    {
        MeterDisplayMode.PowerActiveGauge,
        MeterDisplayMode.PowerActive, MeterDisplayMode.PowerApparent,
        MeterDisplayMode.PowerReactive, MeterDisplayMode.PowerFactor,
        MeterDisplayMode.PowerOutOfBalance
    };

    private static readonly IReadOnlyList<MeterDisplayMode> voltageModes = new[]
    {
        MeterDisplayMode.PhaseVoltageGauge, MeterDisplayMode.PhaseVoltage, MeterDisplayMode.LineVoltage
    };

    private static readonly IReadOnlyList<MeterDisplayMode> currentModes = new[]
    {
        MeterDisplayMode.Current, MeterDisplayMode.CurrentOutOfBalanceGauge, MeterDisplayMode.CurrentOutOfBalance
    };

    private static readonly IReadOnlyList<MeterDisplayMode> moreModes = new[]
    {
        MeterDisplayMode.MoreEnergy, MeterDisplayMode.More
    };

    private int currentPowerModeIndex, currentVoltageModeIndex, currentCurrentIndex, currentMoreIndex;
    private readonly IDataCollectionService? dataCollectionService = IoC.TryGetRegistered<IDataCollectionService>();

    #region Dependency Properties

    public static readonly DependencyProperty SmartMeterProperty = DependencyProperty.Register
    (
        nameof(SmartMeter), typeof(Gen24PowerMeter3P), typeof(SmartMeterControl),
        new PropertyMetadata((d, _) => ((SmartMeterControl)d).SmartMeterDataChanged())
    );

    public Gen24PowerMeter3P? SmartMeter
    {
        get => (Gen24PowerMeter3P?)GetValue(SmartMeterProperty);
        set => SetValue(SmartMeterProperty, value);
    }

    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register
    (
        nameof(Mode), typeof(MeterDisplayMode), typeof(SmartMeterControl),
        new PropertyMetadata(MeterDisplayMode.PowerActiveGauge, (d, _) => ((SmartMeterControl)d).SmartMeterDataChanged())
    );

    public MeterDisplayMode Mode
    {
        get => (MeterDisplayMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public static readonly DependencyProperty MeterStatusProperty = DependencyProperty.Register
    (
        nameof(MeterStatus), typeof(Gen24Status), typeof(SmartMeterControl)
    );

    public Gen24Status? MeterStatus
    {
        get => (Gen24Status?)GetValue(MeterStatusProperty);
        set => SetValue(MeterStatusProperty, value);
    }

    public static readonly DependencyProperty Gen24ConfigProperty = DependencyProperty.Register
    (
        nameof(Gen24Config), typeof(Gen24Config), typeof(SmartMeterControl)
    );

    public Gen24Config? Gen24Config
    {
        get => (Gen24Config?)GetValue(Gen24ConfigProperty);
        set => SetValue(Gen24ConfigProperty, value);
    }

    #endregion

    public SmartMeterControl()
    {
        InitializeComponent();
    }

    private void SmartMeterDataChanged() => Dispatcher.InvokeAsync(() =>
    {
        if (SmartMeter is null)
        {
            BackgroundProvider.Background = Brushes.LightGray;
            Title.Text = "---";
            return;
        }

        Title.Text = $"{SmartMeter.Model} ({dataCollectionService?.HomeAutomationSystem?.Gen24Sensors?.MeterStatus?.StatusMessage ?? Loc.Unknown})";
        BackgroundProvider.Background = dataCollectionService?.HomeAutomationSystem?.Gen24Sensors?.MeterStatus?.ToBrush() ?? Brushes.LightGray;

        Enum.GetNames<MeterDisplayMode>().Apply(enumName =>
        {
            if (GetType().GetField(enumName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)?.GetValue(this) is FrameworkElement element)
            {
                element.Visibility = Mode.ToString() == enumName ? Visibility.Visible : Visibility.Collapsed;
            }
        });
    });

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
