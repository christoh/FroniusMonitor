namespace De.Hochstaetter.FroniusMonitor.Controls;

public partial class Arrow
{
    #region Dpendency Properties

    public static readonly DependencyProperty ArrowLengthProperty = DependencyProperty.Register
    (
        nameof(ArrowLength), typeof(double), typeof(Arrow),
        new PropertyMetadata(100d,(d, _) => ((Arrow)d).OnArrowChanged())
    );

    public double ArrowLength
    {
        get => (double)GetValue(ArrowLengthProperty);
        set => SetValue(ArrowLengthProperty, value);
    }

    public static readonly DependencyProperty FillProperty = DependencyProperty.Register
    (
        nameof(Fill), typeof(Brush), typeof(Arrow)
    );

    public Brush Fill
    {
        get => (Brush)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register
    (
        nameof(Stroke), typeof(Brush), typeof(Arrow)
    );

    public Brush Stroke
    {
        get => (Brush)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register
    (
        nameof(StrokeThickness), typeof(double), typeof(Arrow)
    );

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public static readonly DependencyProperty AngleProperty = DependencyProperty.Register
    (
        nameof(Angle), typeof(double), typeof(Arrow),
        new PropertyMetadata((d, _) => ((Arrow)d).OnArrowChanged())
    );

    public double Angle
    {
        get => (double)GetValue(AngleProperty);
        set => SetValue(AngleProperty, value);
    }

    #endregion

    public Arrow()
    {
        InitializeComponent();
    }

    private void OnArrowChanged()
    {
        Polygon.Points = new PointCollection
        {
            new(10, ArrowLength),
            new(30, ArrowLength),
            new(30, 20),
            new(40, 20),
            new(20, 0),
            new(0, 20),
            new(10, 20),
        };

        RotateTransform.Angle = Angle;
        RotateTransform.CenterX = ViewBox.ActualWidth/2;
        RotateTransform.CenterY = ViewBox.ActualHeight/2;
    }
}