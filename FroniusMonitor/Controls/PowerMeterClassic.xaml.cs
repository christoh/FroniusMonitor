using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.FroniusMonitor.Models;

namespace De.Hochstaetter.FroniusMonitor.Controls
{
    public partial class PowerMeterClassic
    {
        private readonly ThicknessAnimation animation = new();

        public static readonly DependencyProperty SmartMeterControlProperty = DependencyProperty.Register
        (
            nameof(SmartMeter), typeof(Gen24PowerMeter), typeof(PowerMeterClassic),
            new PropertyMetadata((d, e) => ((PowerMeterClassic)d).SmartMeterPropertyChanged())
        );

        public Gen24PowerMeter? SmartMeter
        {
            get => (Gen24PowerMeter?)GetValue(SmartMeterControlProperty);
            set => SetValue(SmartMeterControlProperty, value);
        }

        public PowerMeterClassic()
        {
            InitializeComponent();
        }

        private void SmartMeterPropertyChanged(object? _ = null, PropertyChangedEventArgs? e = null)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (SmartMeter == null)
                {
                    return;
                }

                var power = SmartMeter.RealPowerSum ?? 0;
                var absolutePower = Math.Max(Math.Abs(power), .001);
                var timeSpan = TimeSpan.FromMinutes(600 / absolutePower);
                var leftMargin = Wheel.Margin.Left;
                Wheel.BeginAnimation(MarginProperty, null);

                Wheel.Margin = leftMargin is > 550 or < -80
                    ? new Thickness(power > 0 ? -80 : 550, 0, 0, 0)
                    : new Thickness(leftMargin, 0, 0, 0);

                animation.To = new Thickness(Wheel.Margin.Left + Math.Sign(power) * 630, 0, 0, 0);
                animation.Duration = timeSpan;
                Wheel.BeginAnimation(MarginProperty, animation);
            });
        }

        private async void OnConsumedPowerCalibrated(object _, double value)
        {
            if (SmartMeter is {EnergyRealConsumed: { } energyRealConsumed })
            {
                App.Settings.ConsumedEnergyOffsetWattHours = value - energyRealConsumed;
                await Settings.Save().ConfigureAwait(false);
            }
        }

        private async void OnProducedPowerCalibrated(object _, double value)
        {
            if (SmartMeter is {EnergyRealProduced: { } energyRealProduced } )
            {
                App.Settings.ProducedEnergyOffsetWattHours = value - energyRealProduced;
                await Settings.Save().ConfigureAwait(false);
            }
        }
    }
}
