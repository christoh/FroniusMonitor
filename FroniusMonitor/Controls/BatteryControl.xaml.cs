using System.Windows;
using System.Windows.Media;
using De.Hochstaetter.Fronius.Models;

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

        private void OnStorageChanged()
        {
            var color = Colors.LightGreen;

            if (Storage.StateOfCharge < 50)
            {
                color=Colors.YellowGreen;
            }

            if (Storage.StateOfCharge < 30)
            {
                color = Colors.Yellow;
            }

            if (Storage.StateOfCharge < 20)
            {
                color = Colors.Orange;
            }

            if (Storage.StateOfCharge < 10)
            {
                color = Colors.OrangeRed;
            }

            var height = Storage.StateOfCharge * 800;
            SocPolygon.Points.Clear();
            SocPolygon.Points.Add(new Point(60, 940));
            SocPolygon.Points.Add(new Point(60, 940 - height));
            SocPolygon.Points.Add(new Point(640, 940 - height));
            SocPolygon.Points.Add(new Point(640, 940));
            SocPolygon.Fill = new SolidColorBrush(color);
        }
    }
}
