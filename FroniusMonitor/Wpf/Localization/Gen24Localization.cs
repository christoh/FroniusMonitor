using De.Hochstaetter.FroniusMonitor.Wpf.Markups;

namespace De.Hochstaetter.FroniusMonitor.Wpf.Localization;

public abstract class LocBase : UpdateableMarkupExtension
{
    private readonly Task<string>? task;
    private readonly string category;
    private readonly string? key;

    protected LocBase(string category, string? key, Task<string>? task)
    {
        this.task = task;
        this.category = category;
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

            Task.Run(async () => { UpdateValue(await task); });
        }

        return $"{category}{(key == null ? string.Empty : '.')}{key ?? string.Empty}";
    }
}

public class LocUi : LocBase
{
    public LocUi(string category, string key) : base(category, key, IoC.TryGetRegistered<IWebClientService>()?.GetUiString(category, key)) { }
}

public class LocConfig : LocBase
{
    public LocConfig(string category, string key) : base(category, key, IoC.TryGetRegistered<IWebClientService>()?.GetConfigString(category, key)) { }
}

public class LocChannel : LocBase
{
    public LocChannel(string key) : base(key, null, IoC.TryGetRegistered<IWebClientService>()?.GetChannelString(key)) { }
}
