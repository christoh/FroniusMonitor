using De.Hochstaetter.Fronius.Models.ToshibaAc;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

public partial class HvacSwingModeIcon : Viewbox
{
    public static readonly StyledProperty<ToshibaHvacSwingMode> SwingModeProperty = AvaloniaProperty.Register<HvacSwingModeIcon, ToshibaHvacSwingMode>(nameof(SwingMode), ToshibaHvacSwingMode.Vertical);

    public ToshibaHvacSwingMode SwingMode
    {
        get => GetValue(SwingModeProperty);
        set => SetValue(SwingModeProperty, value);
    }

    public static readonly StyledProperty<IBrush?> FillProperty = AvaloniaProperty.Register<HvacSwingModeIcon, IBrush?>(nameof(Fill), Brushes.Black);

    /// <summary>The hinge and the louver positions that are in play.</summary>
    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public static readonly StyledProperty<IBrush?> InactiveFillProperty = AvaloniaProperty.Register<HvacSwingModeIcon, IBrush?>(nameof(InactiveFill), Brushes.DarkGray);

    /// <summary>The louver positions that are not the fixed one.</summary>
    public IBrush? InactiveFill
    {
        get => GetValue(InactiveFillProperty);
        set => SetValue(InactiveFillProperty, value);
    }

    public static readonly StyledProperty<IBrush?> OffMarkProperty = AvaloniaProperty.Register<HvacSwingModeIcon, IBrush?>(nameof(OffMark), Brushes.Red);

    public IBrush? OffMark
    {
        get => GetValue(OffMarkProperty);
        set => SetValue(OffMarkProperty, value);
    }

    public HvacSwingModeIcon()
    {
        InitializeComponent();
        Update();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SwingModeProperty || change.Property == FillProperty || change.Property == InactiveFillProperty)
        {
            Update();
        }
    }

    private void Update()
    {
        var isFixed = SwingMode is ToshibaHvacSwingMode.Fixed1 or ToshibaHvacSwingMode.Fixed2 or ToshibaHvacSwingMode.Fixed3 or ToshibaHvacSwingMode.Fixed4 or ToshibaHvacSwingMode.Fixed5;

        Fixed1.Stroke = Louver(ToshibaHvacSwingMode.Fixed1);
        Fixed2.Stroke = Louver(ToshibaHvacSwingMode.Fixed2);
        Fixed3.Stroke = Louver(ToshibaHvacSwingMode.Fixed3);
        Fixed4.Stroke = Louver(ToshibaHvacSwingMode.Fixed4);
        Fixed5.Stroke = Louver(ToshibaHvacSwingMode.Fixed5);
        OffRing.IsVisible = OffStroke.IsVisible = SwingMode == ToshibaHvacSwingMode.Off;
        return;

        IBrush? Louver(ToshibaHvacSwingMode position) => !isFixed || SwingMode == position ? Fill : InactiveFill;
    }
}
