using System.Windows.Shapes;
using De.Hochstaetter.FroniusMonitor.AttachedProperties;

namespace De.Hochstaetter.FroniusMonitor.Controls;

public partial class SolarPanels
{
    private static readonly SolidColorBrush solarBrush = new(Color.FromRgb(0xff, 0xd0, 0));
    private static readonly SolidColorBrush panelBrush = new(Color.FromRgb(64, 64, 64));

    public static readonly DependencyProperty TrackerProperty = DependencyProperty.Register
    (
        nameof(Tracker), typeof(Tracker), typeof(SolarPanels),
        new PropertyMetadata((d, e) => ((SolarPanels)d).OnTrackerChanged())
    );

    public Tracker Tracker
    {
        get => (Tracker)GetValue(TrackerProperty);
        set => SetValue(TrackerProperty, value);
    }

    public static readonly DependencyProperty WattPeakProperty = DependencyProperty.Register
    (
        nameof(WattPeak), typeof(double), typeof(SolarPanels)
    );

    public double WattPeak
    {
        get => (double)GetValue(WattPeakProperty);
        set => SetValue(WattPeakProperty, value);
    }

    public SolarPanels()
    {
        InitializeComponent();
    }

    private void OnTrackerChanged()
    {
        this.FindVisualChildren<Rectangle>().Apply(solarPanel => solarPanel.Fill = InverterTracker.GetMppt(solarPanel) == Tracker ? solarBrush : panelBrush);
    }
}