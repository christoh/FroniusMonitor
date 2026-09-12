using De.Hochstaetter.Fronius.Models.ToshibaAc;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

public partial class HvacFanSpeedIcon : Viewbox
{
    private IReadOnlyList<Polygon> levels = [];

    public static readonly StyledProperty<ToshibaHvacFanSpeed> FanSpeedProperty = AvaloniaProperty.Register<HvacFanSpeedIcon, ToshibaHvacFanSpeed>(nameof(FanSpeed), ToshibaHvacFanSpeed.Auto);

    public ToshibaHvacFanSpeed FanSpeed
    {
        get => GetValue(FanSpeedProperty);
        set => SetValue(FanSpeedProperty, value);
    }

    public static readonly StyledProperty<IBrush?> FillProperty = AvaloniaProperty.Register<HvacFanSpeedIcon, IBrush?>(nameof(Fill), Brushes.Black);

    /// <summary>The fan, the bar outlines and the texts.</summary>
    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public static readonly StyledProperty<IBrush?> FanBrushProperty = AvaloniaProperty.Register<HvacFanSpeedIcon, IBrush?>(nameof(FanBrush), Brushes.LimeGreen);

    /// <summary>What the bars up to the set speed are filled with.</summary>
    public IBrush? FanBrush
    {
        get => GetValue(FanBrushProperty);
        set => SetValue(FanBrushProperty, value);
    }

    public static readonly StyledProperty<IBrush?> OffMarkProperty = AvaloniaProperty.Register<HvacFanSpeedIcon, IBrush?>(nameof(OffMark), Brushes.Red);

    /// <summary>The ring of the quiet icon.</summary>
    public IBrush? OffMark
    {
        get => GetValue(OffMarkProperty);
        set => SetValue(OffMarkProperty, value);
    }

    public HvacFanSpeedIcon()
    {
        InitializeComponent();
        levels = [Level1, Level2, Level3, Level4, Level5];
        Update();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FanSpeedProperty || change.Property == FanBrushProperty)
        {
            Update();
        }
    }

    private void Update()
    {
        if (levels.Count == 0)
        {
            return;
        }

        var level = FanSpeed switch
        {
            ToshibaHvacFanSpeed.Manual1 => 0,
            ToshibaHvacFanSpeed.Manual2 => 1,
            ToshibaHvacFanSpeed.Manual3 => 2,
            ToshibaHvacFanSpeed.Manual4 => 3,
            ToshibaHvacFanSpeed.Manual5 => 4,
            _ => -1,
        };

        Quiet.IsVisible = FanSpeed == ToshibaHvacFanSpeed.Quiet;
        Auto.IsVisible = FanSpeed == ToshibaHvacFanSpeed.Auto;

        for (var i = 0; i < levels.Count; i++)
        {
            levels[i].IsVisible = level >= 0;
            levels[i].Fill = i <= level ? FanBrush : Brushes.Transparent;
        }
    }
}
