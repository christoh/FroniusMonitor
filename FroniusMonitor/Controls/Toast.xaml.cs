namespace De.Hochstaetter.FroniusMonitor.Controls;

public partial class Toast
{
    public static DependencyProperty TextProperty = TextBlock.TextProperty.AddOwner
    (
        typeof(Toast),

        new FrameworkPropertyMetadata
        (
            string.Empty,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (d, _) => ((Toast)d).OnTextChanged()
        )
    );

    public static DependencyProperty CornerRadiusProperty = Border.CornerRadiusProperty.AddOwner(typeof(Toast), new PropertyMetadata(new CornerRadius(10)));

    private static readonly DoubleAnimation animation = new(1, 0, TimeSpan.FromMilliseconds(1000)) { AutoReverse = false };

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    static Toast()
    {
       OpacityProperty.OverrideMetadata(typeof(Toast),new FrameworkPropertyMetadata(0d));
    }

    public Toast()
    {
        InitializeComponent();

        animation.Completed+=(_, _) =>
        {
            Text = string.Empty;
        };
    }

    private async void OnTextChanged()
    {
        if (string.IsNullOrWhiteSpace(Text))
        {
            return;
        }

        BeginAnimation(OpacityProperty,null);
        Opacity = 1;
        await Task.Delay(5000);
        BeginAnimation(OpacityProperty,animation);
    }
}