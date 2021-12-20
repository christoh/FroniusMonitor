using System.Windows;
using System.Windows.Controls;
using De.Hochstaetter.Fronius.Models;

namespace De.Hochstaetter.FroniusMonitor.Controls
{
    public partial class PowerMeterClassic : UserControl
    {
        public static readonly DependencyProperty SmartMeterProperty = DependencyProperty.Register
        (
            nameof(SmartMeter), typeof(SmartMeter), typeof(PowerMeterClassic),
            new PropertyMetadata((d, e) => ((PowerMeterClassic)d).OnSmartMeterChanged())
        );

        public SmartMeter SmartMeter
        {
            get => (SmartMeter)GetValue(SmartMeterProperty);
            set => SetValue(SmartMeterProperty, value);
        }

        private void OnSmartMeterChanged()
        { }

        public PowerMeterClassic()
        {
            InitializeComponent();
        }
    }
}
