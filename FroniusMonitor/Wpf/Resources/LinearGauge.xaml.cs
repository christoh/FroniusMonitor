using De.Hochstaetter.FroniusMonitor.Controls;

namespace De.Hochstaetter.FroniusMonitor.Wpf.Resources;

public partial class LinearGauge
{
    #region Dependency Properties

    private static readonly DependencyPropertyDescriptor? valueDescriptor = DependencyPropertyDescriptor.FromProperty(RangeBase.ValueProperty, typeof(MultiColorGauge));
    private static readonly DependencyPropertyDescriptor? minimumDescriptor = DependencyPropertyDescriptor.FromProperty(RangeBase.MinimumProperty, typeof(MultiColorGauge));
    private static readonly DependencyPropertyDescriptor? maximumDescriptor = DependencyPropertyDescriptor.FromProperty(RangeBase.MaximumProperty, typeof(MultiColorGauge));
    private static readonly DependencyPropertyDescriptor? tickFillDescriptor = DependencyPropertyDescriptor.FromProperty(MultiColorGauge.TickFillProperty, typeof(MultiColorGauge));
    private static readonly DependencyPropertyDescriptor? originDescriptor = DependencyPropertyDescriptor.FromProperty(MultiColorGauge.OriginProperty, typeof(MultiColorGauge));
    private static readonly DependencyPropertyDescriptor? showPercentDescriptor = DependencyPropertyDescriptor.FromProperty(MultiColorGauge.ShowPercentProperty, typeof(MultiColorGauge));
    private static readonly DependencyPropertyDescriptor? unitNameDescriptor = DependencyPropertyDescriptor.FromProperty(MultiColorGauge.UnitNameProperty, typeof(MultiColorGauge));
    private static readonly DependencyPropertyDescriptor? stringFormatDescriptor = DependencyPropertyDescriptor.FromProperty(MultiColorGauge.StringFormatProperty, typeof(MultiColorGauge));

    public static readonly DependencyProperty LinearAnimatedValueProperty = DependencyProperty.RegisterAttached
    (
        nameof(GetLinearAnimatedValue)[3..], typeof(double), typeof(MultiColorGauge),
        new PropertyMetadata(OnAnimatedValueChanged)
    );

    public static double GetLinearAnimatedValue(DependencyObject element)
    {
        return (double)element.GetValue(LinearAnimatedValueProperty);
    }

    public static void SetLinearAnimatedValue(DependencyObject element, double value)
    {
        element.SetValue(LinearAnimatedValueProperty, value);
    }

    public static readonly DependencyProperty DisplayUnitNameProperty = DependencyProperty.RegisterAttached
    (
        nameof(GetDisplayUnitName)[3..], typeof(string), typeof(LinearGauge),
        new FrameworkPropertyMetadata(string.Empty)
    );

    public static string? GetDisplayUnitName(DependencyObject element)
    {
        return (string?)element.GetValue(DisplayUnitNameProperty);
    }

    public static void SetDisplayUnitName(DependencyObject element, string? value)
    {
        element.SetValue(DisplayUnitNameProperty, value ?? string.Empty);
    }

    public static readonly DependencyProperty UseAbsoluteValueProperty = DependencyProperty.RegisterAttached
    (
        nameof(GetUseAbsoluteValue)[3..], typeof(bool), typeof(LinearGauge),
        new FrameworkPropertyMetadata(false, OnUseAbsoluteValueChanged)
    );

    public static bool GetUseAbsoluteValue(DependencyObject element)
    {
        return (bool)element.GetValue(UseAbsoluteValueProperty);
    }

    public static void SetUseAbsoluteValue(DependencyObject element, bool value)
    {
        element.SetValue(UseAbsoluteValueProperty, value);
    }

    #endregion

    private record LinearGaugeMetadata(Border InnerBorder, TextBlock ValueTextBlock, Grid Grid, FrameworkElement MidPointMarker);

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
            rootElement.FindName("OuterBorder") is not Border outerBorder ||
            rootElement.FindName("InnerBorder") is not Border innerBorder ||
            rootElement.FindName("Grid") is not Grid grid ||
            rootElement.FindName("MidpointMarker") is not FrameworkElement midPointMarker ||
            rootElement.TemplatedParent is not MultiColorGauge gauge
        )
        {
            return;
        }

        gauge.SetValue(MultiColorGauge.TemplateMetadataPropertyKey, new LinearGaugeMetadata(innerBorder, valueTextBlock, grid, midPointMarker));

        valueDescriptor?.AddValueChanged(gauge, (_, _) => OnValueChanged(gauge, valueTextBlock));
        minimumDescriptor?.AddValueChanged(gauge, (_, _) => OnValueChanged(gauge, valueTextBlock));
        maximumDescriptor?.AddValueChanged(gauge, (_, _) => OnValueChanged(gauge, valueTextBlock));
        originDescriptor?.AddValueChanged(gauge, (_, _) => OnValueChanged(gauge, valueTextBlock));
        showPercentDescriptor?.AddValueChanged(gauge, (_, _) => OnValueChanged(gauge, valueTextBlock));
        tickFillDescriptor?.AddValueChanged(gauge, (_, _) => outerBorder.Background = gauge.TickFill);
        unitNameDescriptor?.AddValueChanged(gauge, (_, _) => SetUnitName(gauge));
        stringFormatDescriptor?.AddValueChanged(gauge, (_, _) => SetValueTextBlock(gauge, valueTextBlock));
        SetUnitName(gauge);
        OnValueChanged(gauge, valueTextBlock, true);
    }

    private static double SetValueTextBlock(MultiColorGauge gauge, TextBlock valueTextBlock)
    {
        var relativeValue = (Math.Max(Math.Min(gauge.Maximum, gauge.Value), gauge.Minimum) - gauge.Minimum) / (gauge.Maximum - gauge.Minimum);

        if (!double.IsFinite(relativeValue))
        {
            relativeValue = 0;
        }
        var value = gauge.ShowPercent ? GetUseAbsoluteValue(gauge) ? (relativeValue - gauge.Origin) * 100 / gauge.Origin : relativeValue * 100 : gauge.Value; //BUG: Only works if Origin is at 0
        valueTextBlock.Text = value.ToString(gauge.StringFormat, CultureInfo.CurrentCulture);
        return relativeValue;
    }

    private static void OnValueChanged(MultiColorGauge gauge, TextBlock valueTextBlock, bool skipAnimation = false)
    {
        var relativeValue = SetValueTextBlock(gauge, valueTextBlock);

        if (skipAnimation)
        {
            gauge.BeginAnimation(LinearAnimatedValueProperty, null);
            SetLinearAnimatedValue(gauge, relativeValue);
            OnAnimatedValueChanged(gauge, new DependencyPropertyChangedEventArgs(LinearAnimatedValueProperty, null, relativeValue));
            return;
        }

        var animation = new DoubleAnimation(relativeValue, gauge.AnimationDuration)
        {
            AccelerationRatio = .33,
            DecelerationRatio = .33,
        };

        gauge.BeginAnimation(LinearAnimatedValueProperty, animation);
    }

    private static void OnUseAbsoluteValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var gauge = (MultiColorGauge)d;

        if (gauge.TemplateMetadata is LinearGaugeMetadata metadata)
        {
            OnValueChanged(gauge, metadata.ValueTextBlock);
        }
    }

    private static void SetUnitName(MultiColorGauge gauge)
    {
        SetDisplayUnitName(gauge, gauge.ShowPercent ? "%" : gauge.UnitName);
    }

    private static void OnAnimatedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if
        (
            d is not MultiColorGauge gauge ||
            e.NewValue is not double animatedValue ||
            gauge.TemplateMetadata is not LinearGaugeMetadata metadata
        )
        {
            return;
        }

        var (innerBorder, _, grid, midpointMarker) = metadata;
        var width = grid.Width * Math.Abs(animatedValue - gauge.Origin);

        midpointMarker.Visibility = gauge.Origin > .00001 ? Visibility.Visible : Visibility.Hidden;
        midpointMarker.Margin = new Thickness(gauge.Origin * grid.Width - midpointMarker.Width / 2, 0, 0, 0);

        innerBorder.Width = width;
        innerBorder.Margin = new Thickness(animatedValue > gauge.Origin ? grid.Width * gauge.Origin : grid.Width * gauge.Origin - width, 0, 0, 0);


        var baseColor = MultiColorGauge.GetColorForRelativeValue(gauge, animatedValue);
        var brush = new LinearGradientBrush(baseColor, baseColor * 0.4f + Colors.Black * 0.6f, 90);
        brush.Freeze();
        innerBorder.Background = brush;
    }
}
