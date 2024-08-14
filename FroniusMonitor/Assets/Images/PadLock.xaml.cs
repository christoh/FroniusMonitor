namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class PadLock
{
    public static readonly DependencyProperty CableLockStatusProperty = DependencyProperty.Register
    (
        nameof(CableLockStatus), typeof(CableLockStatus), typeof(PadLock),
        new PropertyMetadata((d, e) => ((PadLock)d).OnCableLockStatusChanged())
    );

    public CableLockStatus CableLockStatus
    {
        get => (CableLockStatus)GetValue(CableLockStatusProperty);
        set => SetValue(CableLockStatusProperty, value);
    }


    public PadLock()
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

        MainBody.Stroke = CableLockStatus switch
        {
            CableLockStatus.Unknown => Brushes.DarkGray,
            CableLockStatus.LockFailed => Brushes.Red,
            CableLockStatus.LockUnlockPowerOut => Brushes.DarkOrange,
            CableLockStatus.Locked => Brushes.DarkGreen,
            CableLockStatus.Unlocked => Brushes.DarkGreen,
            CableLockStatus.UnlockFailed => Brushes.Red,
            _ => throw new NotSupportedException(),
        };
    }
}