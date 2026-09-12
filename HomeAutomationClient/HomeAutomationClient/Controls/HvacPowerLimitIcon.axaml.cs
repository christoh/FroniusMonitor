namespace De.Hochstaetter.HomeAutomationClient.Controls;

public partial class HvacPowerLimitIcon : Viewbox
{
    public static readonly StyledProperty<byte> PowerLimitProperty = AvaloniaProperty.Register<HvacPowerLimitIcon, byte>(nameof(PowerLimit), 100);

    /// <summary>Percent; the device knows 50, 75 and 100.</summary>
    public byte PowerLimit
    {
        get => GetValue(PowerLimitProperty);
        set => SetValue(PowerLimitProperty, value);
    }

    public static readonly StyledProperty<IBrush?> FillProperty = AvaloniaProperty.Register<HvacPowerLimitIcon, IBrush?>(nameof(Fill), Brushes.Black);

    /// <summary>The caption and the block outlines.</summary>
    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public static readonly StyledProperty<IBrush?> PowerBrushProperty = AvaloniaProperty.Register<HvacPowerLimitIcon, IBrush?>(nameof(PowerBrush), Brushes.LimeGreen);

    /// <summary>What a lit block is filled with.</summary>
    public IBrush? PowerBrush
    {
        get => GetValue(PowerBrushProperty);
        set => SetValue(PowerBrushProperty, value);
    }

    public HvacPowerLimitIcon()
    {
        InitializeComponent();
        Update();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PowerLimitProperty || change.Property == PowerBrushProperty)
        {
            Update();
        }
    }

    private void Update()
    {
        P75.Background = PowerLimit < 75 ? Brushes.Transparent : PowerBrush;
        P100.Background = PowerLimit < 100 ? Brushes.Transparent : PowerBrush;
    }
}
