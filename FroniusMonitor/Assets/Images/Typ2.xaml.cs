namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class Typ2
{
    public static readonly DependencyProperty WattPilotProperty = DependencyProperty.Register
    (
        nameof(WattPilot), typeof(WattPilot), typeof(Typ2),
        new PropertyMetadata((d, _) => ((Typ2)d).OnWattPilotChanged())
    );

    public WattPilot? WattPilot
    {
        get => (WattPilot)GetValue(WattPilotProperty);
        set => SetValue(WattPilotProperty, value);
    }

    private void OnWattPilotChanged() { }

    public Typ2()
    {
        InitializeComponent();
    }
}