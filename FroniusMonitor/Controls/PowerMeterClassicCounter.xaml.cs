using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace De.Hochstaetter.FroniusMonitor.Controls
{
    public partial class PowerMeterClassicCounter
    {
        private IReadOnlyList<TextBlock> textBlocks;

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register
        (
            nameof(Value), typeof(double), typeof(PowerMeterClassicCounter),
            new PropertyMetadata((d, e) => ((PowerMeterClassicCounter)d).OnValueChanged())
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
            var value = (int)Math.Round(Value,MidpointRounding.AwayFromZero);

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
    }
}
