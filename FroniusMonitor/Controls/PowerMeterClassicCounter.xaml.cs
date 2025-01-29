namespace De.Hochstaetter.FroniusMonitor.Controls;

public partial class PowerMeterClassicCounter
{
    private readonly IReadOnlyList<TextBlock> textBlocks;
    private double calibratedValue;
    private readonly DoubleAnimation animation = new(1, TimeSpan.FromMilliseconds(200));

    public event EventHandler<double>? CalibrationCompleted;

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register
    (
        nameof(Value), typeof(double), typeof(PowerMeterClassicCounter),
        new PropertyMetadata((d, _) => ((PowerMeterClassicCounter)d).OnValueChanged())
    );

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public PowerMeterClassicCounter()
    {
        InitializeComponent();
        textBlocks = Canvas.Children.OfType<Border>().Select(b => (TextBlock)b.Child).ToArray();
    }

    private bool IsInCalibration
    {
        get;
        set
        {
            field = value;

            switch (value)
            {
                case true:
                    animation.To = 1;
                    MinusScaler.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                    PlusScaler.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                    ButtonScaler.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                    break;

                case false:
                    animation.To = 0;
                    MinusScaler.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                    PlusScaler.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                    ButtonScaler.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                    break;
            }
        }
    }

    private void OnValueChanged()
    {
        var value = (int)Math.Round(IsInCalibration ? calibratedValue : Value, MidpointRounding.AwayFromZero);

        for (byte i = 0; i < textBlocks.Count; i++)
        {
            var digitString = (value % 10).ToString("D1", CultureInfo.CurrentCulture);
            value /= 10;
            textBlocks[i].Text = value == 0 && i > 3 && digitString == "0" ? string.Empty : digitString;
        }
    }

    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e is { ClickCount: > 1, ChangedButton: MouseButton.Left })
        {
            calibratedValue = Value;
            IsInCalibration = true;
        }
    }

    private void OnOkPressed(object sender, RoutedEventArgs e)
    {
        CalibrationCompleted?.Invoke(this, calibratedValue);
        OnCancelPressed(sender, e);
    }

    private void OnCancelPressed(object sender, RoutedEventArgs e)
    {
        IsInCalibration = false;
        OnValueChanged();
    }

    private void OnMinusPressed(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var subtrahend = Math.Pow(10, (int)button.Tag);
        calibratedValue -= subtrahend;

        if (calibratedValue < 0)
        {
            calibratedValue += subtrahend;
        }

        OnValueChanged();
    }

    private void OnPlusPressed(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var addend = Math.Pow(10, (int)button.Tag);
        calibratedValue += addend;

        if (calibratedValue > 999_999_999)
        {
            calibratedValue -= addend;
        }

        OnValueChanged();
    }
}