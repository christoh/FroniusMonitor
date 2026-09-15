using De.Hochstaetter.Fronius.Models.Charging;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

/// <summary>
/// The Type 2 socket of a Wattpilot. Takes the charger and lets the XAML bind each contact to the phase flags and
/// currents through the phase converters; nothing is painted from here.
/// </summary>
public partial class Typ2Control : UserControl
{
    public static readonly StyledProperty<WattPilot?> WattPilotProperty = AvaloniaProperty.Register<Typ2Control, WattPilot?>(nameof(WattPilot));

    public WattPilot? WattPilot
    {
        get => GetValue(WattPilotProperty);
        set => SetValue(WattPilotProperty, value);
    }

    public Typ2Control()
    {
        InitializeComponent();
    }
}
