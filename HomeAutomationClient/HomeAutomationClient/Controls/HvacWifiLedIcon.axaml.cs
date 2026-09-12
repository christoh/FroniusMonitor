using De.Hochstaetter.Fronius.Models.ToshibaAc;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

public partial class HvacWifiLedIcon : Viewbox
{
    public static readonly StyledProperty<ToshibaHvacWifiLedStatus> LedStatusProperty = AvaloniaProperty.Register<HvacWifiLedIcon, ToshibaHvacWifiLedStatus>(nameof(LedStatus), ToshibaHvacWifiLedStatus.Off);

    public ToshibaHvacWifiLedStatus LedStatus
    {
        get => GetValue(LedStatusProperty);
        set => SetValue(LedStatusProperty, value);
    }

    public static readonly StyledProperty<IBrush?> FillProperty = AvaloniaProperty.Register<HvacWifiLedIcon, IBrush?>(nameof(Fill), Brushes.Black);

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public static readonly StyledProperty<IBrush?> OnBrushProperty = AvaloniaProperty.Register<HvacWifiLedIcon, IBrush?>(nameof(OnBrush), Brushes.LimeGreen);

    public IBrush? OnBrush
    {
        get => GetValue(OnBrushProperty);
        set => SetValue(OnBrushProperty, value);
    }

    public static readonly StyledProperty<IBrush?> OffBrushProperty = AvaloniaProperty.Register<HvacWifiLedIcon, IBrush?>(nameof(OffBrush), Brushes.DarkGray);

    public IBrush? OffBrush
    {
        get => GetValue(OffBrushProperty);
        set => SetValue(OffBrushProperty, value);
    }

    public HvacWifiLedIcon()
    {
        InitializeComponent();
        Update();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LedStatusProperty || change.Property == OnBrushProperty || change.Property == OffBrushProperty)
        {
            Update();
        }
    }

    private void Update() => Led.Fill = LedStatus == ToshibaHvacWifiLedStatus.On ? OnBrush : OffBrush;
}
