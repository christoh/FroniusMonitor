namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class Speaker
{
    public static readonly DependencyProperty LevelProperty = DependencyProperty.Register
    (
        nameof(Level), typeof(ToshibaHvacMeritFeaturesA), typeof(Speaker)
    );

    public ToshibaHvacMeritFeaturesA Level
    {
        get => (ToshibaHvacMeritFeaturesA)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    public Speaker()
    {
        InitializeComponent();
    }
}
