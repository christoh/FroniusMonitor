using De.Hochstaetter.Fronius.Models.ToshibaAc;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

public partial class HvacMeritFeatureIcon : Viewbox
{
    public static readonly StyledProperty<ToshibaHvacMeritFeaturesA> MeritFeaturesAProperty = AvaloniaProperty.Register<HvacMeritFeatureIcon, ToshibaHvacMeritFeaturesA>(nameof(MeritFeaturesA));

    public ToshibaHvacMeritFeaturesA MeritFeaturesA
    {
        get => GetValue(MeritFeaturesAProperty);
        set => SetValue(MeritFeaturesAProperty, value);
    }

    public static readonly StyledProperty<IBrush?> FillProperty = AvaloniaProperty.Register<HvacMeritFeatureIcon, IBrush?>(nameof(Fill), Brushes.Black);

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public static readonly StyledProperty<IBrush?> OffMarkProperty = AvaloniaProperty.Register<HvacMeritFeatureIcon, IBrush?>(nameof(OffMark), Brushes.Red);

    /// <summary>The ring around the silent modes.</summary>
    public IBrush? OffMark
    {
        get => GetValue(OffMarkProperty);
        set => SetValue(OffMarkProperty, value);
    }

    public HvacMeritFeatureIcon()
    {
        InitializeComponent();
        Update();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == MeritFeaturesAProperty)
        {
            Update();
        }
    }

    private void Update()
    {
        Heating8C.IsVisible = MeritFeaturesA == ToshibaHvacMeritFeaturesA.Heating8C;
        Eco.IsVisible = MeritFeaturesA == ToshibaHvacMeritFeaturesA.Eco;
        Normal.IsVisible = MeritFeaturesA == ToshibaHvacMeritFeaturesA.None;
        HighPower.IsVisible = MeritFeaturesA == ToshibaHvacMeritFeaturesA.HighPower;
        Floor.IsVisible = MeritFeaturesA == ToshibaHvacMeritFeaturesA.Floor;
        Silent.IsVisible = MeritFeaturesA is ToshibaHvacMeritFeaturesA.Silent1 or ToshibaHvacMeritFeaturesA.Silent2;
        Speaker.Level = MeritFeaturesA;
    }
}
