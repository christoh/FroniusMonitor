using De.Hochstaetter.Fronius.Models.ToshibaAc;

namespace De.Hochstaetter.HomeAutomationClient.Assets.Images;

/// <summary>
/// The speaker of the Toshiba silent modes. Silent 1 is the louder of the two and shows both arcs, Silent 2 only
/// the inner one; any other level shows a mute speaker.
/// </summary>
public partial class SpeakerIcon : Viewbox
{
    public static readonly StyledProperty<IBrush?> FillProperty = AvaloniaProperty.Register<SpeakerIcon, IBrush?>(nameof(Fill), Brushes.Black);

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public static readonly StyledProperty<ToshibaHvacMeritFeaturesA> LevelProperty = AvaloniaProperty.Register<SpeakerIcon, ToshibaHvacMeritFeaturesA>(nameof(Level));

    public ToshibaHvacMeritFeaturesA Level
    {
        get => GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    public SpeakerIcon()
    {
        InitializeComponent();
        Update();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LevelProperty)
        {
            Update();
        }
    }

    private void Update()
    {
        InnerArc.IsVisible = Level is ToshibaHvacMeritFeaturesA.Silent1 or ToshibaHvacMeritFeaturesA.Silent2;
        OuterArc.IsVisible = Level is ToshibaHvacMeritFeaturesA.Silent1;
    }
}
