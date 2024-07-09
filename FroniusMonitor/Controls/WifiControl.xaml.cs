using System.Windows.Shapes;

namespace De.Hochstaetter.FroniusMonitor.Controls;

public partial class WifiControl
{
    public static readonly DependencyProperty FillProperty = Shape.FillProperty.AddOwner
    (
        typeof(WifiControl)
    );

    public Brush Fill
    {
        get => (Brush)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public static readonly DependencyProperty SignalStrengthProperty = DependencyProperty.Register
    (
        nameof(SignalStrength), typeof(double), typeof(WifiControl),
        new PropertyMetadata(-50d, (d, _) => ((WifiControl)d).OnSignalStrengthChanged())
    );

    public double SignalStrength
    {
        get => (double)GetValue(SignalStrengthProperty);
        set => SetValue(SignalStrengthProperty, value);
    }

    public static readonly DependencyProperty IsConnectedProperty = DependencyProperty.Register
    (
        nameof(IsConnected), typeof(bool), typeof(WifiControl),
        new PropertyMetadata(true, (d, _) => ((WifiControl)d).OnIsConnectedChanged())
    );

    public bool IsConnected
    {
        get => (bool)GetValue(IsConnectedProperty);
        set => SetValue(IsConnectedProperty, value);
    }

    private void OnIsConnectedChanged()
    {
            Connection.Opacity = !IsConnected ? 0.3 : 1;
        }

    public WifiControl()
    {
            InitializeComponent();
        }

    private void OnSignalStrengthChanged()
    {
            High.Opacity = !IsConnected || SignalStrength < -52 ? 0.3 : 1;
            Medium.Opacity = !IsConnected || SignalStrength < -58 ? 0.3 : 1;
            Low.Opacity = !IsConnected || SignalStrength < -65 ? 0.3 : 1;
        }
}