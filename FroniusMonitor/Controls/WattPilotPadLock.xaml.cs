namespace De.Hochstaetter.FroniusMonitor.Controls;

public partial class WattPilotPadLock
{
    public static readonly DependencyProperty CableLockStatusProperty = DependencyProperty.Register
    (
        nameof(CableLockStatus), typeof(CableLockStatus), typeof(WattPilotPadLock),
        new PropertyMetadata((d, _) => ((WattPilotPadLock)d).OnCableLockStatusChanged())
    );

    public CableLockStatus CableLockStatus
    {
        get => (CableLockStatus)GetValue(CableLockStatusProperty);
        set => SetValue(CableLockStatusProperty, value);
    }


    public WattPilotPadLock()
    {
        InitializeComponent();
    }

    private void OnCableLockStatusChanged()
    {
        Locked.Visibility = CableLockStatus switch
        {
            CableLockStatus.Unknown => Visibility.Collapsed,
            CableLockStatus.LockFailed => Visibility.Visible,
            CableLockStatus.LockUnlockPowerOut => Visibility.Collapsed,
            CableLockStatus.Locked => Visibility.Visible,
            CableLockStatus.Unlocked => Visibility.Collapsed,
            CableLockStatus.UnlockFailed => Visibility.Collapsed,
            _ => throw new NotSupportedException(),
        };

        Unlocked.Visibility = CableLockStatus switch
        {
            CableLockStatus.Unknown => Visibility.Visible,
            CableLockStatus.LockFailed => Visibility.Collapsed,
            CableLockStatus.LockUnlockPowerOut => Visibility.Visible,
            CableLockStatus.Locked => Visibility.Collapsed,
            CableLockStatus.Unlocked => Visibility.Visible,
            CableLockStatus.UnlockFailed => Visibility.Visible,
            _ => throw new NotSupportedException(),
        };

        MainBody.Fill = CableLockStatus switch
        {
            CableLockStatus.Unknown => Brushes.DarkGray,
            CableLockStatus.LockFailed => Brushes.OrangeRed,
            CableLockStatus.LockUnlockPowerOut => Brushes.DarkOrange,
            CableLockStatus.Locked => Brushes.ForestGreen,
            CableLockStatus.Unlocked => Brushes.ForestGreen,
            CableLockStatus.UnlockFailed => Brushes.OrangeRed,
            _ => throw new NotSupportedException(),
        };
    }
}