namespace De.Hochstaetter.FroniusMonitor.Unity;

public static class IoC
{
    public static T TryGet<T>()
    {
        try
        {
            return App.Container.Resolve<T>();
        }
        catch
        {
            return default(T)!;
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
}