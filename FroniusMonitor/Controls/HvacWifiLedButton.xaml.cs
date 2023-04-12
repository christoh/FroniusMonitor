namespace De.Hochstaetter.FroniusMonitor.Controls;

public partial class HvacWifiLedButton
{
    public static readonly DependencyProperty LedStatusProperty = DependencyProperty.Register
    (
        nameof(LedStatus), typeof(ToshibaHvacWifiLedStatus), typeof(HvacWifiLedButton)
    );

    public ToshibaHvacWifiLedStatus LedStatus
    {
        get => (ToshibaHvacWifiLedStatus)GetValue(LedStatusProperty);
        set => SetValue(LedStatusProperty, value);
    }

    public HvacWifiLedButton()
    {
        InitializeComponent();
    }
}