namespace De.Hochstaetter.FroniusMonitor.Controls
{
    public class HvacButton : Button
    {
        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register
        (
            nameof(Mode), typeof(ToshibaHvacOperatingMode), typeof(HvacButton) /*,
            new PropertyMetadata((d, _) => ((HvacButton)d).OnModeChanged())*/
        );

        public ToshibaHvacOperatingMode Mode
        {
            get => (ToshibaHvacOperatingMode)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }
    }
}
