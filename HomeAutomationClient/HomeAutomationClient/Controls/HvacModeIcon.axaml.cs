using De.Hochstaetter.Fronius.Models.ToshibaAc;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

public partial class HvacModeIcon : Viewbox
{
    public static readonly StyledProperty<ToshibaHvacOperatingMode> ModeProperty = AvaloniaProperty.Register<HvacModeIcon, ToshibaHvacOperatingMode>(nameof(Mode), ToshibaHvacOperatingMode.Auto);

    public ToshibaHvacOperatingMode Mode
    {
        get => GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<HvacModeIcon, bool>(nameof(IsSelected));

    /// <summary>The ring is filled with <see cref="SelectedFill"/> while the device is in this mode.</summary>
    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public static readonly StyledProperty<bool> IsHoveredProperty = AvaloniaProperty.Register<HvacModeIcon, bool>(nameof(IsHovered));

    /// <summary>
    /// The pointer is over the button this icon sits in; an unselected ring is then filled with <see cref="HoverFill"/>,
    /// as the WPF template did on IsMouseOver. Set from a style on the button's :pointerover, so the icon itself
    /// handles no pointer.
    /// </summary>
    public bool IsHovered
    {
        get => GetValue(IsHoveredProperty);
        set => SetValue(IsHoveredProperty, value);
    }

    public static readonly StyledProperty<IBrush?> HoverFillProperty = AvaloniaProperty.Register<HvacModeIcon, IBrush?>(nameof(HoverFill), new SolidColorBrush(Color.Parse("#4000c0ff")));

    public IBrush? HoverFill
    {
        get => GetValue(HoverFillProperty);
        set => SetValue(HoverFillProperty, value);
    }

    public static readonly StyledProperty<IBrush?> FillProperty = AvaloniaProperty.Register<HvacModeIcon, IBrush?>(nameof(Fill), Brushes.Black);

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public static readonly StyledProperty<IBrush?> SelectedFillProperty = AvaloniaProperty.Register<HvacModeIcon, IBrush?>(nameof(SelectedFill), Brushes.Aquamarine);

    public IBrush? SelectedFill
    {
        get => GetValue(SelectedFillProperty);
        set => SetValue(SelectedFillProperty, value);
    }

    public HvacModeIcon()
    {
        InitializeComponent();
        Update();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ModeProperty || change.Property == IsSelectedProperty || change.Property == SelectedFillProperty || change.Property == IsHoveredProperty || change.Property == HoverFillProperty)
        {
            Update();
        }
    }

    private void Update()
    {
        Ring.Fill = IsSelected ? SelectedFill : IsHovered ? HoverFill : Brushes.Transparent;
        FanOnly.IsVisible = Mode == ToshibaHvacOperatingMode.FanOnly;
        Cooling.IsVisible = Mode == ToshibaHvacOperatingMode.Cooling;
        Heating.IsVisible = Mode == ToshibaHvacOperatingMode.Heating;
        Drying.IsVisible = Mode == ToshibaHvacOperatingMode.Drying;
        Auto.IsVisible = Mode == ToshibaHvacOperatingMode.Auto;
    }
}
