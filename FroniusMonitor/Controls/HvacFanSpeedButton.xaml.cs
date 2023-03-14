namespace De.Hochstaetter.FroniusMonitor.Controls
{
    public partial class HvacFanSpeedButton
    {
        private readonly IReadOnlyList<Border> manualElements;
        private readonly IReadOnlyList<UIElement> allElements;

        public static readonly DependencyProperty FanSpeedProperty = DependencyProperty.Register
        (
            nameof(FanSpeed), typeof(ToshibaAcFanSpeed), typeof(HvacFanSpeedButton),
            new PropertyMetadata((d, e) => ((HvacFanSpeedButton)d).OnFanSpeedChanged(e))
        );

        public ToshibaAcFanSpeed FanSpeed
        {
            get => (ToshibaAcFanSpeed)GetValue(FanSpeedProperty);
            set => SetValue(FanSpeedProperty, value);
        }

        public HvacFanSpeedButton()
        {
            InitializeComponent();
            manualElements = new[] { Level1, Level2, Level3, Level4, Level5, };
            allElements = new[] { (UIElement)QuietIcon, Auto, }.Concat(manualElements).ToArray();
        }

        private void DisableAll()
        {
            allElements.Apply(element => element.Visibility = Visibility.Collapsed);
        }

        private void OnFanSpeedChanged(DependencyPropertyChangedEventArgs e)
        {
            DisableAll();

            switch (FanSpeed)
            {
                case ToshibaAcFanSpeed.Quiet:
                    QuietIcon.Visibility = Visibility.Visible;
                    break;

                case ToshibaAcFanSpeed.Auto:
                    Auto.Visibility = Visibility.Visible;
                    break;

                default:
                    manualElements.Apply(element => element.Visibility = Visibility.Visible);

                    var level = FanSpeed switch
                    {
                        ToshibaAcFanSpeed.Manual1 => 0,
                        ToshibaAcFanSpeed.Manual2 => 1,
                        ToshibaAcFanSpeed.Manual3 => 2,
                        ToshibaAcFanSpeed.Manual4 => 3,
                        ToshibaAcFanSpeed.Manual5 => 4,
                        _ => -1,
                    };

                    if (level >= 0)
                    {
                        for (var i = 0; i < manualElements.Count; i++)
                        {
                            manualElements[i].Background = i <= level ? Foreground : Brushes.Transparent;
                        }
                    }

                    break;
            }
        }
    }
}
