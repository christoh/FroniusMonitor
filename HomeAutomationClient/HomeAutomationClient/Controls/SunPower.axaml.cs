namespace De.Hochstaetter.HomeAutomationClient.Controls
{
    public partial class SunPower : ContentControl
    {
        public static readonly StyledProperty<double> PeakPowerProperty = AvaloniaProperty.Register<SunPower, double>(nameof(PeakPower));

        public double PeakPower
        {
            get => GetValue(PeakPowerProperty);
            set => SetValue(PeakPowerProperty, value);
        }

        public SunPower()
        {
            InitializeComponent();
        }
    }
}
