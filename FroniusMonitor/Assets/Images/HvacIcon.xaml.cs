namespace De.Hochstaetter.FroniusMonitor.Assets.Images
{
    public partial class HvacIcon
    {
        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register
        (
            nameof(Mode), typeof(ToshibaAcOperatingMode), typeof(HvacIcon),
            new PropertyMetadata((d, e) => ((HvacIcon)d).OnModeChanged())
        );

        public ToshibaAcOperatingMode Mode
        {
            get => (ToshibaAcOperatingMode)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        private void OnModeChanged() { }

        public HvacIcon()
        {
            InitializeComponent();
        }
    }
}
