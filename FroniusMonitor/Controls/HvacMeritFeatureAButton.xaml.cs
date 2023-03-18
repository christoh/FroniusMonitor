namespace De.Hochstaetter.FroniusMonitor.Controls;

public partial class HvacMeritFeatureAButton
{
    public static readonly DependencyProperty MeritFeaturesProperty = DependencyProperty.Register
    (
        nameof(MeritFeatures), typeof(ushort), typeof(HvacMeritFeatureAButton)/*,
        new PropertyMetadata((d, e) => ((HvacMeritFeatureAButton)d).OnMeritFeaturesChanged())*/
    );

    public ushort MeritFeatures
    {
        get => (ushort)GetValue(MeritFeaturesProperty);
        set => SetValue(MeritFeaturesProperty, value);
    }

    public static readonly DependencyProperty MeritFeaturesAProperty = DependencyProperty.Register
    (
        nameof(MeritFeaturesA), typeof(ToshibaHvacMeritFeaturesA), typeof(HvacMeritFeatureAButton),
        new PropertyMetadata((d, _) => ((HvacMeritFeatureAButton)d).OnMeritFeaturesAChanged())
    );

    public ToshibaHvacMeritFeaturesA MeritFeaturesA
    {
        get => (ToshibaHvacMeritFeaturesA)GetValue(MeritFeaturesAProperty);
        set => SetValue(MeritFeaturesAProperty, value);
    }

    public HvacMeritFeatureAButton()
    {
        InitializeComponent();
    }

    private void OnMeritFeaturesAChanged() { }
}