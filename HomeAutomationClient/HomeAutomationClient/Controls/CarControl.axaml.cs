using De.Hochstaetter.Fronius.Models.Charging;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

/// <summary>
/// The car on the Wattpilot card. Takes the three values it shows; the status becomes pseudo classes so that the
/// colours and the pulsing live in the styles of the XAML (see there), which is the one thing a code behind may do
/// with UI state.
/// </summary>
public partial class CarControl : UserControl
{
    private const string Idle = ":idle";
    private const string Charging = ":charging";
    private const string WaitCar = ":waitcar";
    private const string Complete = ":complete";
    private const string Error = ":error";

    public static readonly StyledProperty<CarStatus?> StatusProperty = AvaloniaProperty.Register<CarControl, CarStatus?>(nameof(Status));

    public CarStatus? Status
    {
        get => GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    /// <summary>The energy of the current charging session, in watt hours.</summary>
    public static readonly StyledProperty<double?> EnergyWattHoursProperty = AvaloniaProperty.Register<CarControl, double?>(nameof(EnergyWattHours));

    public double? EnergyWattHours
    {
        get => GetValue(EnergyWattHoursProperty);
        set => SetValue(EnergyWattHoursProperty, value);
    }

    /// <summary>Whose card started the session, written on the door.</summary>
    public static readonly StyledProperty<string?> CurrentUserProperty = AvaloniaProperty.Register<CarControl, string?>(nameof(CurrentUser));

    public string? CurrentUser
    {
        get => GetValue(CurrentUserProperty);
        set => SetValue(CurrentUserProperty, value);
    }

    public CarControl()
    {
        InitializeComponent();
        UpdatePseudoClasses();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == StatusProperty)
        {
            UpdatePseudoClasses();
        }
    }

    // Nothing known counts as idle, as in the WPF converters: the cross and the dimmed text say "no car".
    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(Idle, Status is CarStatus.Idle or null);
        PseudoClasses.Set(Charging, Status == CarStatus.Charging);
        PseudoClasses.Set(WaitCar, Status == CarStatus.WaitCar);
        PseudoClasses.Set(Complete, Status == CarStatus.Complete);
        PseudoClasses.Set(Error, Status == CarStatus.Error);
    }
}
