using System.Windows.Shapes;

namespace De.Hochstaetter.FroniusMonitor.Controls;

public sealed partial class SolarPanels
{
    private Brush solarBrush = new SolidColorBrush(Color.FromRgb(0xff, 0xd0, 0));
    private Brush panelBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64));

    public static readonly DependencyProperty TrackerProperty = DependencyProperty.Register
    (
        nameof(Tracker), typeof(Tracker), typeof(SolarPanels),
        new PropertyMetadata((d, _) => ((SolarPanels)d).OnTrackerChanged())
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

        if (DesignerProperties.GetIsInDesignMode(this))
        {
            return;
        }
        
        OnSettingsChanged(this);
        Loaded += (_, _) => App.Settings.SettingsChanged += OnSettingsChanged;
        Unloaded += (_, _) => App.Settings.SettingsChanged -= OnSettingsChanged;
    }

    public void OnSettingsChanged(object? sender, EventArgs? __ = null) => Dispatcher.Invoke(() =>
    {
        FrameworkElement content;

        try
        {
            using var stream = new FileStream(App.Settings.CustomSolarPanelLayout!, FileMode.Open, FileAccess.Read, FileShare.Read);
            content = (FrameworkElement)XamlReader.Load(stream);
        }
        catch
        {
            content = new SolarPanel { Foreground = Brushes.DarkGreen };
        }

        content.DataContext = this;

        if (content.TryFindResource("ActiveSolarPanelBrush") is Brush activeSolarPanelBrush)
        {
            solarBrush = activeSolarPanelBrush;
        }

        if (content.TryFindResource("InactiveSolarPanelBrush") is Brush inactiveSolarPanelBrush)
        {
            panelBrush = inactiveSolarPanelBrush;
        }

        Child = content;

        if (!ReferenceEquals(sender, this))
        {
            OnTrackerChanged();
        }
    });

    private void OnTrackerChanged()
    {
        this.FindVisualChildren<Shape>()
            .Where(InverterTracker.GetColorShapes)
            .Apply(solarPanel => solarPanel.Fill = InverterTracker.GetMppt(solarPanel) == Tracker ? solarBrush : panelBrush);
    }
}