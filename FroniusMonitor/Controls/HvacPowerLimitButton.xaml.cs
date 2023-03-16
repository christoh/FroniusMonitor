namespace De.Hochstaetter.FroniusMonitor.Controls;

public partial class HvacPowerLimitButton
{
    public static readonly DependencyProperty PowerBrushProperty = DependencyProperty.Register
    (
        nameof(PowerBrush), typeof(Brush), typeof(HvacPowerLimitButton), new PropertyMetadata (Brushes.Black)
    );

    public Brush PowerBrush
    {
        get => (Brush)GetValue(PowerBrushProperty);
        set => SetValue(PowerBrushProperty, value);
    }

    public static readonly DependencyProperty PowerBorderBrushProperty = DependencyProperty.Register
    (
        nameof(PowerBorderBrush), typeof(Brush), typeof(HvacPowerLimitButton)
    );

    public Brush PowerBorderBrush
    {
        get => (Brush)GetValue(PowerBorderBrushProperty);
        set => SetValue(PowerBorderBrushProperty, value);
    }

    public static readonly DependencyProperty PowerLimitProperty = DependencyProperty.Register
    (
        nameof(PowerLimit), typeof(byte), typeof(HvacPowerLimitButton),
        new PropertyMetadata((d, _) => ((HvacPowerLimitButton)d).OnPowerLimitChanged())
    );

    public byte PowerLimit
    {
        get => (byte)GetValue(PowerLimitProperty);
        set => SetValue(PowerLimitProperty, value);
    }

    public HvacPowerLimitButton()
    {
        InitializeComponent();
    }

    private void OnPowerLimitChanged()
    {
        PowerBrush.Freeze();
        P75.Background = PowerLimit < 75 ? Brushes.Transparent : PowerBrush;
        P100.Background = PowerLimit < 100 ? Brushes.Transparent : PowerBrush;
    }

}