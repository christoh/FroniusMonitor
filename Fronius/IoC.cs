namespace De.Hochstaetter.Fronius
{
    public class IoC : IServiceProvider
    {
        public static IServiceProvider? Injector { get; set; }

        public static T Get<T>() => (T)(Get(typeof(T)) ?? default(T))!;

        public static object? Get(Type type) => Injector == null ? throw new NullReferenceException($"App must set an {nameof(Injector)} that implements {nameof(IServiceProvider)}") : Injector.GetService(type);

        public object? GetService(Type serviceType) => Get(serviceType);
    }
}
