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

        private bool isInChargingAnimation;

        private void OnStorageChanged()
        {
            if (Storage.Power > 10)
            {
                if (!isInChargingAnimation)
                {
                    isInChargingAnimation = true;
                    PlusPole.Background = Enclosure.BorderBrush = new SolidColorBrush(Colors.DarkGreen);

                    var animation = new ColorAnimation
                    {
                        To = Color.FromRgb(0, 136, 178),
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever,
                        Duration = TimeSpan.FromSeconds(1.5),
                    };
                    
                    PlusPole.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);
                }
            }
            else
            {
                var batteryBrush = Storage.TrafficLight switch
                {
                    TrafficLight.Red => Brushes.Red,
                    TrafficLight.Green => Brushes.DarkGreen,
                    _ => Brushes.DarkGray
                };

                isInChargingAnimation = false;
                PlusPole.Background = Enclosure.BorderBrush = batteryBrush;
            }


            var brush = Brushes.LightGreen;

            if (Storage.StateOfCharge < 0.5)
            {
                brush = Brushes.YellowGreen;
            }

            if (Storage.StateOfCharge < 0.3)
            {
                brush = Brushes.Yellow;
            }

            if (Storage.StateOfCharge < 0.2)
            {
                brush = Brushes.Orange;
            }

            if (Storage.StateOfCharge < 0.12)
            {
                brush = Brushes.OrangeRed;
            }

            if (Storage.StateOfCharge < 0.08)
            {
                brush = Brushes.Red;
            }

            var height = Storage.StateOfCharge * 800;
            SocPolygon.Points.Clear();
            SocPolygon.Points.Add(new Point(60, 940));
            SocPolygon.Points.Add(new Point(60, 940 - height));
            SocPolygon.Points.Add(new Point(640, 940 - height));
            SocPolygon.Points.Add(new Point(640, 940));
            SocPolygon.Fill = brush;
        }
    }
}
