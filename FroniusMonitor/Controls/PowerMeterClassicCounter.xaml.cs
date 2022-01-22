using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace De.Hochstaetter.FroniusMonitor.Controls;

public partial class PowerMeterClassicCounter
{
    private readonly IReadOnlyList<TextBlock> textBlocks;
    private bool isInCalibration;
    private double calibratedValue;

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

    private void OnValueChanged()
    {
        var value = (int)Math.Round(isInCalibration ? calibratedValue : Value, MidpointRounding.AwayFromZero);

        foreach (var textBlock in textBlocks)
        {
            var text = (value % 10).ToString("D1");
            value /= 10;

            if (text == "0" && value == 0)
            {
                text = "";
            }

            textBlock.Text = text;
        }
    }

    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount > 1 && e.ChangedButton == MouseButton.Left)
        {
            calibratedValue = Value;
            isInCalibration = true;
            MinusCanvas.Visibility = PlusCanvas.Visibility = ButtonPanel.Visibility = Visibility.Visible;
        }
    }

    private void OnOkPressed(object sender, RoutedEventArgs e)
    {
        CalibrationCompleted?.Invoke(this, calibratedValue);
        OnCancelPressed(sender, e);
    }

    private void OnCancelPressed(object sender, RoutedEventArgs e)
    {
        MinusCanvas.Visibility = PlusCanvas.Visibility = ButtonPanel.Visibility = Visibility.Collapsed;
        isInCalibration = false;
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