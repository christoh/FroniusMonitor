using System.Reflection;

namespace De.Hochstaetter.FroniusMonitor.Wpf.Markups;

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
