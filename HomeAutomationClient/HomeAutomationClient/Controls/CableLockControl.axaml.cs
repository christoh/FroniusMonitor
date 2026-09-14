using De.Hochstaetter.Fronius.Models.Charging;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

/// <summary>
/// The padlock beside the cable's rating on the Wattpilot card. Turns the <see cref="CableLockStatus"/> into pseudo
/// classes; what they look like is in the XAML. Nothing known is drawn open and gray, as the WPF control did.
/// </summary>
public partial class CableLockControl : UserControl
{
    private const string Closed = ":closed";
    private const string Ok = ":ok";
    private const string Failed = ":failed";
    private const string PowerOut = ":powerout";

    public static readonly StyledProperty<CableLockStatus?> CableLockStatusProperty = AvaloniaProperty.Register<CableLockControl, CableLockStatus?>(nameof(CableLockStatus));

    public CableLockStatus? CableLockStatus
    {
        get => GetValue(CableLockStatusProperty);
        set => SetValue(CableLockStatusProperty, value);
    }

    public CableLockControl()
    {
        InitializeComponent();
        UpdatePseudoClasses();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CableLockStatusProperty)
        {
            UpdatePseudoClasses();
        }
    }

    private void UpdatePseudoClasses()
    {
        // A failed lock shows the shackle closed: that is what the charger tried to do, and what the colour then
        // says went wrong.
        PseudoClasses.Set(Closed, CableLockStatus is Fronius.Models.Charging.CableLockStatus.Locked or Fronius.Models.Charging.CableLockStatus.LockFailed);
        PseudoClasses.Set(Ok, CableLockStatus is Fronius.Models.Charging.CableLockStatus.Locked or Fronius.Models.Charging.CableLockStatus.Unlocked);
        PseudoClasses.Set(Failed, CableLockStatus is Fronius.Models.Charging.CableLockStatus.LockFailed or Fronius.Models.Charging.CableLockStatus.UnlockFailed);
        PseudoClasses.Set(PowerOut, CableLockStatus == Fronius.Models.Charging.CableLockStatus.LockUnlockPowerOut);
    }
}
