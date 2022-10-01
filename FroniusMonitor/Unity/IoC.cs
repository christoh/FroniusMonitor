namespace De.Hochstaetter.FroniusMonitor.Unity;

public class IoC:IServiceProvider
{
    public static T? TryGet<T>()
    {
        try
        {
            return App.Container.Resolve<T>();
        }
        catch
        {
            return default;
        }
    }
    public static T Get<T>()
    {
        return App.Container.Resolve<T>();
    }

    public static object Get(Type type)
    {
        return App.Container.Resolve(type);
    }

    public object GetService(Type serviceType) => Get(serviceType);
}