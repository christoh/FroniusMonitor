using De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

/// <summary>A box and a slider for one <see cref="NumberField"/>. See the markup for why the field is a property.</summary>
public partial class NumberSlider : UserControl
{
    public static readonly StyledProperty<NumberField?> FieldProperty = AvaloniaProperty.Register<NumberSlider, NumberField?>(nameof(Field));

    public static readonly StyledProperty<string> CaptionProperty = AvaloniaProperty.Register<NumberSlider, string>(nameof(Caption), string.Empty);

    public static readonly StyledProperty<string> UnitProperty = AvaloniaProperty.Register<NumberSlider, string>(nameof(Unit), string.Empty);

    /// <summary>The width of the box. 40 fits three digits; a decimal or a fourth digit wants 60.</summary>
    public static readonly StyledProperty<double> BoxWidthProperty = AvaloniaProperty.Register<NumberSlider, double>(nameof(BoxWidth), 40);

    public NumberSlider() => InitializeComponent();

    public NumberField? Field
    {
        get => GetValue(FieldProperty);
        set => SetValue(FieldProperty, value);
    }

    public string Caption
    {
        get => GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public string Unit
    {
        get => GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public double BoxWidth
    {
        get => GetValue(BoxWidthProperty);
        set => SetValue(BoxWidthProperty, value);
    }
}
