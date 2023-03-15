using System.Windows.Shapes;

namespace De.Hochstaetter.FroniusMonitor.Controls
{
    public partial class HvacFanSpeedButton
    {
        private readonly IReadOnlyList<Shape> manualElements;
        private readonly IReadOnlyList<UIElement> allElements;

        #region DependencyProperties

        public static readonly DependencyProperty FanSpeedProperty = DependencyProperty.Register
        (
            nameof(FanSpeed), typeof(ToshibaAcFanSpeed), typeof(HvacFanSpeedButton),
            new PropertyMetadata((d, _) => ((HvacFanSpeedButton)d).OnFanSpeedChanged())
        );

        public ToshibaAcFanSpeed FanSpeed
        {
            get => (ToshibaAcFanSpeed)GetValue(FanSpeedProperty);
            set => SetValue(FanSpeedProperty, value);
        }

        public static readonly DependencyProperty FanBrushProperty = DependencyProperty.Register
        (
            nameof(FanBrush), typeof(Brush), typeof(HvacFanSpeedButton),
            new PropertyMetadata((d, e) => ((HvacFanSpeedButton)d).OnFanSpeedChanged())
        );

        public Brush FanBrush
        {
            get => (Brush)GetValue(FanBrushProperty);
            set => SetValue(FanBrushProperty, value);
        }

        #endregion

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

        private void OnFanSpeedChanged()
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
                            manualElements[i].Fill = i <= level ? FanBrush : Brushes.Transparent;
                        }
                    }

                    break;
            }
        }
    }
}
