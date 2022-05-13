namespace De.Hochstaetter.FroniusMonitor.Controls;

public partial class LcdDisplay
{
    #region Dependency Properties

    public static readonly DependencyProperty Label1Property = DependencyProperty.Register
    (
        nameof(Label1), typeof(string), typeof(LcdDisplay)
    );

    public string? Label1
    {
        get => (string?)GetValue(Label1Property);
        set => SetValue(Label1Property, value);
    }

    public static readonly DependencyProperty Label2Property = DependencyProperty.Register
    (
        nameof(Label2), typeof(string), typeof(LcdDisplay)
    );

    public string? Label2
    {
        get => (string?)GetValue(Label2Property);
        set => SetValue(Label2Property, value);
    }

    public static readonly DependencyProperty Label3Property = DependencyProperty.Register
    (
        nameof(Label3), typeof(string), typeof(LcdDisplay)
    );

    public string? Label3
    {
        get => (string?)GetValue(Label3Property);
        set => SetValue(Label3Property, value);
    }

    public static readonly DependencyProperty LabelSumProperty = DependencyProperty.Register
    (
        nameof(LabelSum), typeof(string), typeof(LcdDisplay)
    );

    public string? LabelSum
    {
        get => (string?)GetValue(LabelSumProperty);
        set => SetValue(LabelSumProperty, value);
    }

    public static readonly DependencyProperty Value1Property = DependencyProperty.Register
    (
        nameof(Value1), typeof(string), typeof(LcdDisplay)

    );

    public string? Value1
    {
        get => (string?)GetValue(Value1Property);
        set => SetValue(Value1Property, value);
    }

    public static readonly DependencyProperty Value2Property = DependencyProperty.Register
    (
        nameof(Value2), typeof(string), typeof(LcdDisplay)
    );

    public string? Value2
    {
        get => (string?)GetValue(Value2Property);
        set => SetValue(Value2Property, value);
    }

    public static readonly DependencyProperty Value3Property = DependencyProperty.Register
    (
        nameof(Value3), typeof(string), typeof(LcdDisplay)
    );

    public string? Value3
    {
        get => (string?)GetValue(Value3Property);
        set => SetValue(Value3Property, value);
    }

    public static readonly DependencyProperty ValueSumProperty = DependencyProperty.Register
    (
        nameof(ValueSum), typeof(string), typeof(LcdDisplay)

    );

    public string? ValueSum
    {
        get => (string?)GetValue(ValueSumProperty);
        set => SetValue(ValueSumProperty, value);
    }

    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register
    (
        nameof(Header), typeof(string), typeof(LcdDisplay)
    );

    public string? Header
    {
        get => (string?)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    #endregion

    public LcdDisplay()
    {
        InitializeComponent();
    }
}
