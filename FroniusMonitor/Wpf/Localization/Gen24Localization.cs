using De.Hochstaetter.FroniusMonitor.Wpf.Markups;

namespace De.Hochstaetter.FroniusMonitor.Wpf.Localization;

public abstract class LocBase(string? key, Task<string>? task, bool doNotShowKey = false) : UpdateableMarkupExtension
{
    protected override object ProvideUpdateableValue(IServiceProvider serviceProvider)
    {
        if (task is not null)
        {
            if (task.Status != TaskStatus.WaitingForActivation && TargetProperty is not DependencyProperty)
            {
                return task.GetAwaiter().GetResult();
            }

            Task.Run(async () => { UpdateValue(await task.ConfigureAwait(false)); });
        }

        return doNotShowKey || key == null ? string.Empty : key;
    }
}

public class Config(string path, bool doNotShowKey) : LocBase(path, IoC.TryGetRegistered<IGen24Service>()?.GetConfigString(path), doNotShowKey)
{
    public Config(string path) : this(path, false) { }
}

public class Ui(string path, bool doNotShowKey) : LocBase(path, IoC.TryGetRegistered<IGen24Service>()?.GetUiString(path), doNotShowKey)
{
    public Ui(string path) : this(path, false) { }
}

public class Channel(string key, bool doNotShowKey) : LocBase(key, IoC.TryGetRegistered<IGen24Service>()?.GetChannelString(key), doNotShowKey)
{
    public Channel(string path) : this(path, false) { }
}