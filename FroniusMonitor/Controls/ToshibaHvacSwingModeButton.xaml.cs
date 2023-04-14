namespace De.Hochstaetter.FroniusMonitor.Controls;

public partial class ToshibaHvacSwingModeButton
{

    public static readonly DependencyProperty SwingModeProperty = DependencyProperty.Register
    (
        nameof(SwingMode), typeof(ToshibaHvacSwingMode), typeof(ToshibaHvacSwingModeButton)
    );

    public ToshibaHvacSwingMode SwingMode
    {
        get => (ToshibaHvacSwingMode)GetValue(SwingModeProperty);
        set => SetValue(SwingModeProperty, value);
    }

    public ToshibaHvacSwingModeButton()
    {
        InitializeComponent();
    }
}