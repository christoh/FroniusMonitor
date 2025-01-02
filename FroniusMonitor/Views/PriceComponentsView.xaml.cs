namespace De.Hochstaetter.FroniusMonitor.Views;

public partial class PriceComponentsView
{
    public List<AwattarPriceComponent> PriceComponents { get; }

    public PriceComponentsView(List<AwattarPriceComponent> priceComponents)
    {
        PriceComponents = priceComponents;
        InitializeComponent();
    }
}