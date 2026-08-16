using De.Hochstaetter.HomeAutomationClient.Extensions;

namespace De.Hochstaetter.HomeAutomationClient.Assets.Images;

public partial class GridIcon : Viewbox
{
    public static readonly StyledProperty<IBrush?> FillProperty= AvaloniaProperty.Register<GridIcon, IBrush?>(nameof(Fill));
    
    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public GridIcon()
    {
        InitializeComponent();
        Fill ??= Application.Current!.GetSolidColorBrush("PowerGridIcon");
    }
}