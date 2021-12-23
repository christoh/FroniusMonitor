using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using De.Hochstaetter.Fronius.Models;

namespace De.Hochstaetter.FroniusMonitor.Controls
{
    public partial class PowerMeterClassic
    {
        private readonly ThicknessAnimation animation = new();

        public static readonly DependencyProperty SmartMeterControlProperty = DependencyProperty.Register
        (
            nameof(SmartMeter), typeof(SmartMeter), typeof(PowerMeterClassic),
            new PropertyMetadata((d, e) => ((PowerMeterClassic)d).OnSmartMeterChanged(e))
        );

        public SmartMeter? SmartMeter
        {
            get => (SmartMeter?)GetValue(SmartMeterControlProperty);
            set => SetValue(SmartMeterControlProperty, value);
        }

        public PowerMeterClassic()
        {
            InitializeComponent();

            Unloaded += (_, _) =>
            {
                if (SmartMeter is not null)
                {
                    SmartMeter.PropertyChanged -= SmartMeterPropertyChanged;
                }
            };
        }

        private void OnSmartMeterChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is SmartMeter oldSmartMeter)
            {
                oldSmartMeter.PropertyChanged -= SmartMeterPropertyChanged;
            }

            if (SmartMeter != null)
            {
                SmartMeter.PropertyChanged += SmartMeterPropertyChanged;
                SmartMeterPropertyChanged();
            }
        }

        private void SmartMeterPropertyChanged(object? _ = null, PropertyChangedEventArgs? e = null)
        {
            if (e != null && e.PropertyName != nameof(SmartMeter.Data))
            {
                return;
            }

            Dispatcher.InvokeAsync(() =>
            {
                var data = SmartMeter?.Data;

                if (data is null)
                {
                    return;
                }

                var power = SmartMeter?.Data?.TotalRealPower ?? 0;
                var absolutePower = Math.Max(Math.Abs(power), .001);
                var timeSpan= TimeSpan.FromMinutes(600 / absolutePower);
                var leftMargin = Wheel.Margin.Left;
                Wheel.BeginAnimation(MarginProperty,null);

                Wheel.Margin = leftMargin is > 550 or < -80
                    ? new Thickness(power > 0 ? -80 : 550, 0, 0, 0)
                    : new Thickness(leftMargin, 0, 0, 0);
                
                animation.To = new Thickness(Wheel.Margin.Left + Math.Sign(power) * 630, 0, 0, 0);
                animation.Duration = timeSpan;
                Wheel.BeginAnimation(MarginProperty, animation);
            });
        }
    }
}
