namespace De.Hochstaetter.FroniusMonitor.AttachedProperties;

public enum Tracker : sbyte
{
    Unknown = -1,
    None = 0,
    Inverter1Mppt1 = 1,
    Inverter1Mppt2 = 2,
    Inverter2Mppt1 = 3,
    Inverter2Mppt2 = 4,
}

public class InverterTracker
{
    public static readonly DependencyProperty MpptProperty = DependencyProperty.RegisterAttached
    (
        "Mppt", typeof(Tracker), typeof(InverterTracker),
        new FrameworkPropertyMetadata(Tracker.Unknown, FrameworkPropertyMetadataOptions.Inherits)
    );

    public static void SetMppt(UIElement element, Tracker value)
    {
        element.SetValue(MpptProperty, value);
    }

    public static Tracker GetMppt(UIElement element)
    {
        return (Tracker)element.GetValue(MpptProperty);
    }
    
    public static readonly DependencyProperty ColorShapesProperty = DependencyProperty.RegisterAttached
    (
        "ColorShapes", typeof(bool), typeof(InverterTracker),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits)
    );

    public static void SetColorShapes(UIElement element, bool value)
    {
        element.SetValue(ColorShapesProperty, value);
    }

    public static bool GetColorShapes(UIElement element)
    {
        return (bool)element.GetValue(ColorShapesProperty);
    }
}
