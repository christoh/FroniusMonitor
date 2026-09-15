namespace De.Hochstaetter.HomeAutomationServer.Models.Settings;

public class SettingsChangeTracker
{
    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    public void NotifySettingsChanged<T>(IOptionsMonitor<T> options)
    {
        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(options.CurrentValue));
    }
}
