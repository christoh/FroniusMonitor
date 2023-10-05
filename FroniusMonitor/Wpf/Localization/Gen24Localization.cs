using De.Hochstaetter.FroniusMonitor.Wpf.Markups;

namespace De.Hochstaetter.FroniusMonitor.Wpf.Localization;

public abstract class LocBase : UpdateableMarkupExtension
{
    private readonly Task<string>? task;
    private readonly string? key;

    protected LocBase(string? key, Task<string>? task)
    {
        this.task = task;
        this.key = key;
    }

    protected override object ProvideUpdateableValue(IServiceProvider serviceProvider)
    {
        if (task is not null)
        {
            if (task.Status != TaskStatus.WaitingForActivation && TargetProperty is not DependencyProperty)
            {
                return task.Result;
            }

            Task.Run(async () => { UpdateValue(await task.ConfigureAwait(false)); });
        }

        return key ?? string.Empty;
    }
}

public class Config : LocBase
{
    public Config(string path) : base(path, IoC.TryGetRegistered<IGen24Service>()?.GetConfigString(path)) { }
}

public class Ui : LocBase
{
    public Ui(string path) : base(path, IoC.TryGetRegistered<IGen24Service>()?.GetUiString(path)) { }
}

public class Channel : LocBase
{
    public Channel(string key) : base(key, IoC.TryGetRegistered<IGen24Service>()?.GetChannelString(key)) { }
}
