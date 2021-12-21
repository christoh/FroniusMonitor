using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using De.Hochstaetter.Fronius.Models;

namespace De.Hochstaetter.FroniusMonitor.Controls;

public partial class StorageControl
{
    private bool isInChargingAnimation;

    private static readonly ColorAnimation chargingAnimation = new()
    {
        To = Color.FromRgb(0, 136, 178),
        AutoReverse = true,
        RepeatBehavior = RepeatBehavior.Forever,
        Duration = TimeSpan.FromSeconds(1.5),
    };

    public static readonly DependencyProperty StorageProperty = DependencyProperty.Register
    (
        nameof(Storage), typeof(Storage), typeof(StorageControl),
        new PropertyMetadata((d, e) => ((StorageControl)d).OnStorageChanged())
    );

    public Storage Storage
    {
        get => (Storage)GetValue(StorageProperty);
        set => SetValue(StorageProperty, value);
    }

    public StorageControl()
    {
        InitializeComponent();
    }

    private void OnStorageChanged()
    {
        Brush brush;
        var data = Storage.Data;

        if (data == null)
        {
            return;
        }

        if (data.StateOfCharge < 0.08)
        {
            brush = Brushes.Red;
        }
        else if (data.StateOfCharge < 0.12)
        {
            brush = Brushes.OrangeRed;
        }
        else if (data.StateOfCharge < 0.2)
        {
            brush = Brushes.Orange;
        }
        else if (data.StateOfCharge < 0.3)
        {
            brush = Brushes.Yellow;
        }
        else if (data.StateOfCharge < 0.5)
        {
            brush = Brushes.YellowGreen;
        }
        else
        {
            brush = Brushes.LightGreen;
        }

        SocRectangle.Height = data.StateOfCharge * BackgroundRectangle.Height;
        SocRectangle.Fill = brush;

        if (data.Power > 10)
        {
            if (!isInChargingAnimation)
            {
                isInChargingAnimation = true;
                PlusPole.Background = Enclosure.BorderBrush = new SolidColorBrush(Colors.DarkGreen);
                PlusPole.Background.BeginAnimation(SolidColorBrush.ColorProperty, chargingAnimation);
            }
        }
        else
        {
            if (!PlusPole.Background.IsFrozen)
            {
                PlusPole.Background.BeginAnimation(SolidColorBrush.ColorProperty, null);
            }

            brush = data.TrafficLight switch
            {
                TrafficLight.Red => Brushes.Red,
                TrafficLight.Green => Brushes.DarkGreen,
                _ => Brushes.DarkGray
            };

            isInChargingAnimation = false;
            PlusPole.Background = Enclosure.BorderBrush = brush;
        }
    }
}