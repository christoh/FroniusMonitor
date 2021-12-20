using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using De.Hochstaetter.Fronius.Models;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace De.Hochstaetter.FroniusMonitor.Controls
{
    public partial class BatteryControl
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
            nameof(Storage), typeof(Storage), typeof(BatteryControl),
            new PropertyMetadata((d, e) => ((BatteryControl)d).OnStorageChanged())
        );

        public Storage Storage
        {
            get => (Storage)GetValue(StorageProperty);
            set => SetValue(StorageProperty, value);
        }

        public BatteryControl()
        {
            InitializeComponent();
        }

        private void OnStorageChanged()
        {
            Brush brush;

            if (Storage.StateOfCharge < 0.08)
            {
                brush = Brushes.Red;
            }
            else if (Storage.StateOfCharge < 0.12)
            {
                brush = Brushes.OrangeRed;
            }
            else if (Storage.StateOfCharge < 0.2)
            {
                brush = Brushes.Orange;
            }
            else if (Storage.StateOfCharge < 0.3)
            {
                brush = Brushes.Yellow;
            }
            else if (Storage.StateOfCharge < 0.5)
            {
                brush = Brushes.YellowGreen;
            }
            else
            {
                brush = Brushes.LightGreen;
            }

            SocRectangle.Height = Storage.StateOfCharge * BackgroundRectangle.ActualHeight;
            SocRectangle.Fill = brush;

            if (Storage.Power > 10)
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

                brush = Storage.TrafficLight switch
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
}
