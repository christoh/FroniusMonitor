namespace De.Hochstaetter.FroniusMonitor.Controls;

public partial class House
{
    public static readonly DependencyProperty ConsumptionWattsProperty = DependencyProperty.Register
    (
        nameof(ConsumptionWatts), typeof(double?), typeof(House)
    );

    public double? ConsumptionWatts
    {
        get => (double?)GetValue(ConsumptionWattsProperty);
        set => SetValue(ConsumptionWattsProperty, value);
    }

    public static readonly DependencyProperty PowerLossWattsProperty = DependencyProperty.Register
    (
        nameof(PowerLossWatts), typeof(double?), typeof(House)
    );

    public double? PowerLossWatts
    {
        get => (double?)GetValue(PowerLossWattsProperty);
        set => SetValue(PowerLossWattsProperty, value);
    }

    public static readonly DependencyProperty EfficiencyProperty = DependencyProperty.Register
    (
        nameof(Efficiency), typeof(double?), typeof(House)
    );

    public double? Efficiency
    {
        get => (double)GetValue(EfficiencyProperty);
        set => SetValue(EfficiencyProperty, value);
    }

    public static readonly DependencyProperty ProductionWattsProperty = DependencyProperty.Register
    (
        nameof(ProductionWatts), typeof(double?), typeof(House),
        new PropertyMetadata((d, e) => ((House)d).OnProductionWattsChanged())
    );

    public double? ProductionWatts
    {
        get => (double?)GetValue(ProductionWattsProperty);
        set => SetValue(ProductionWattsProperty, value);
    }

    private void OnProductionWattsChanged() { }

    public House()
    {
        InitializeComponent();
    }
}