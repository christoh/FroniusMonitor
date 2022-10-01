using System.Reflection;

namespace De.Hochstaetter.FroniusMonitor.Wpf.Localization;

public abstract class UpdateableMarkupExtension : MarkupExtension
{
    private object? targetObject;
    private object? targetProperty;

    protected object? TargetObject => targetObject;

    protected object? TargetProperty => targetProperty;

    public sealed override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget target)
        {
            targetObject = target.TargetObject;
            targetProperty = target.TargetProperty;
            return ProvideUpdateableValue(serviceProvider);
        }

        return this;
    }

    protected void UpdateValue(object value)
    {
        if (targetObject == null)
        {
            return;
        }

        if (targetProperty is DependencyProperty dp)
        {
            var d = (DependencyObject)targetObject;
            void UpdateAction() => d.SetValue(dp, value);

            if (d.CheckAccess())
            {
                UpdateAction();
            }
            else
            {
                d.Dispatcher.Invoke(UpdateAction);
            }
        }
        else if (targetProperty is PropertyInfo propertyInfo)
        {
            propertyInfo.SetValue(targetObject, value, null);
        }
    }

    protected abstract object ProvideUpdateableValue(IServiceProvider serviceProvider);
}

public abstract class LocBase : UpdateableMarkupExtension
{
    private readonly Task<string>? task;
    private readonly string category;
    private readonly string key;

    protected LocBase(string category, string key, Task<string>? task)
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

        return $"{category}.{key}";
    }
}

public class LocUi : LocBase
{
    public LocUi(string category, string key) : base(category, key, IoC.TryGet<IWebClientService>()?.GetUiString(category, key)) { }
}

public class LocConfig : LocBase
{
    public LocConfig(string category, string key) : base(category, key, IoC.TryGet<IWebClientService>()?.GetConfigString(category, key)) { }
}
