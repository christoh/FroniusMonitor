using System.Windows.Shapes;

namespace De.Hochstaetter.FroniusMonitor.Controls;

public partial class PowerFlowArrow
{
    public static readonly DependencyProperty PowerProperty = DependencyProperty.Register
    (
        nameof(Power), typeof(double?), typeof(PowerFlowArrow),
        new PropertyMetadata((d, _) => ((PowerFlowArrow)d).OnPowerChanged())
    );

    public double? Power
    {
        get => (double?)GetValue(PowerProperty);
        set => SetValue(PowerProperty, value);
    }

    public static readonly DependencyProperty FillProperty = Shape.FillProperty.AddOwner
    (
        typeof(PowerFlowArrow)
    );

    public Brush Fill
    {
        get => (Brush)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public static readonly DependencyProperty HasRightPlacementProperty = DependencyProperty.Register
    (
        nameof(HasRightPlacement), typeof(bool), typeof(PowerFlowArrow),
        new PropertyMetadata((d, _) => ((PowerFlowArrow)d).OnPowerChanged())
    );

    public bool HasRightPlacement
    {
        get => (bool)GetValue(HasRightPlacementProperty);
        set => SetValue(HasRightPlacementProperty, value);
    }

    public static readonly DependencyProperty DefaultsToOutgoingProperty = DependencyProperty.Register
    (
        nameof(DefaultsToOutgoing), typeof(bool), typeof(PowerFlowArrow),
        new PropertyMetadata((d, _) => ((PowerFlowArrow)d).OnPowerChanged())
    );

    public bool DefaultsToOutgoing
    {
        get => (bool)GetValue(DefaultsToOutgoingProperty);
        set => SetValue(DefaultsToOutgoingProperty, value);
    }

    public static readonly DependencyProperty AngleProperty = RotateTransform.AngleProperty.AddOwner
    (
        typeof(PowerFlowArrow)
    );

    public double Angle
    {
        get => (double)GetValue(AngleProperty);
        set => SetValue(AngleProperty, value);
    }

    public static readonly DependencyProperty TextPlacementProperty = DependencyProperty.Register
    (
        nameof(TextPlacement), typeof(PlacementMode), typeof(PowerFlowArrow),
        new PropertyMetadata(PlacementMode.Center /*,(d, _) => ((PowerFlowArrow)d).OnTextPlacementChanged()*/)
    );

    public PlacementMode TextPlacement
    {
        get => (PlacementMode)GetValue(TextPlacementProperty);
        set => SetValue(TextPlacementProperty, value);
    }

    //private void OnTextPlacementChanged() { }

    public PowerFlowArrow()
    {
        InitializeComponent();
    }

    private void OnPowerChanged()
    {
        TextBlock.Text = !Power.HasValue || Power.Value == 0d ? "--- W" : $"{Math.Abs(Power.Value):N1} W";
        var sign = HasRightPlacement ? 1 : -1;
        //Angle="{Binding PowerString, Converter={co:PowerDirectionToDouble Incoming=-90, Outgoing=90, Null=-90}, FallbackValue=90}" 
        Arrow.Angle = Power < 0 ? 90 * sign : Power > 0 ? -90 * sign : DefaultsToOutgoing ? 90 * sign : -90 * sign;
    }
}
