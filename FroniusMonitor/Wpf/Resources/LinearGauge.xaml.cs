namespace De.Hochstaetter.FroniusMonitor.Wpf.Resources;

public partial class LinearGauge
{
    #region Dependency Properties

    private static readonly DependencyPropertyDescriptor? valueDescriptor = DependencyPropertyDescriptor.FromProperty(RangeBase.ValueProperty, typeof(MultiColorGauge));

    public static readonly DependencyProperty LabelProperty = DependencyProperty.RegisterAttached
    (
        nameof(GetLabel)[3..], typeof(string), typeof(LinearGauge),
        new FrameworkPropertyMetadata(string.Empty)
    );

    public static string? GetLabel(DependencyObject element)
    {
        return (string?)element.GetValue(LabelProperty);
    }

    public static void SetLabel(DependencyObject element, string? value)
    {
        element.SetValue(LabelProperty, value ?? string.Empty);
    }

    public static readonly DependencyProperty UnitNameProperty = DependencyProperty.RegisterAttached
    (
        nameof(GetUnitName)[3..], typeof(string), typeof(LinearGauge),
        new FrameworkPropertyMetadata(string.Empty)
    );

    public static string? GetUnitName(DependencyObject element)
    {
        return (string?)element.GetValue(UnitNameProperty);
    }

    public static void SetUnitName(DependencyObject element, string? value)
    {
        element.SetValue(UnitNameProperty, value ?? string.Empty);
    }

    public static readonly DependencyProperty StringFormatProperty = DependencyProperty.RegisterAttached
    (
        nameof(GetStringFormat)[3..], typeof(string), typeof(LinearGauge),
        new FrameworkPropertyMetadata(string.Empty)
    );

    public static string? GetStringFormat(DependencyObject element)
    {
        return (string?)element.GetValue(StringFormatProperty);
    }

    public static void SetStringFormat(DependencyObject element, string? value)
    {
        element.SetValue(StringFormatProperty, value ?? string.Empty);
    }

    #endregion

    public LinearGauge()
    {
        InitializeComponent();
    }

    private void OnRootElementInitialized(object? sender, EventArgs e)
    {
        if
        (
            sender is not FrameworkElement rootElement ||
            rootElement.FindName("ValueTextBlock") is not TextBlock valueTextBlock ||
            rootElement.TemplatedParent is not MultiColorGauge gauge
        )
        {
            return;
        }

        //RangeBase.ValueProperty.OverrideMetadata(typeof(MultiColorGauge), new FrameworkPropertyMetadata(0d, (_, _) => OnValueChanged(gauge, valueTextBlock)));

        valueDescriptor?.AddValueChanged(gauge, (_, _) => OnValueChanged(gauge, valueTextBlock));
        OnValueChanged(gauge, valueTextBlock);
    }

    private static void OnValueChanged(MultiColorGauge gauge, TextBlock valueTextBlock)
    {
        valueTextBlock.Text = gauge.Value.ToString(GetStringFormat(gauge), CultureInfo.CurrentCulture);
    }
}
