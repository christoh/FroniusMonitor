namespace De.Hochstaetter.FroniusMonitor.Controls
{
    public class HvacButton : Button
    {
        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register
        (
            nameof(Mode), typeof(ToshibaAcOperatingMode), typeof(HvacButton) /*,
            new PropertyMetadata((d, _) => ((HvacButton)d).OnModeChanged())*/
        );

        public ToshibaAcOperatingMode Mode
        {
            get => (ToshibaAcOperatingMode)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }
    }
}
