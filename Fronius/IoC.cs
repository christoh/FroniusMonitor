namespace De.Hochstaetter.Fronius
{
    public class IoC : IServiceProvider
    {
        public static IServiceProvider? Injector { get; private set; }

        public static T Get<T>() => (T)Get(typeof(T));
        public static T? TryGetRegistered<T>() => (T?)TryGetRegistered(typeof(T));
        public static T? TryGet<T>() => (T?)TryGet(typeof(T));
        public static T GetRegistered<T>() => (T)GetRegistered(typeof(T));

        public static object? TryGetRegistered(Type type) => Injector?.GetService(type);

        public static object Get(Type type) => Injector == null
            ? throw new NullReferenceException($"App must set an {nameof(Injector)} that implements {nameof(IServiceProvider)}")
            : Injector.GetService(type)
              ?? Activator.CreateInstance(type)
              ?? throw new InvalidOperationException($"Cannot inject {type.Name}: Requires registration or a parameter-less constructor");

        public static object? TryGet(Type type)
        {
            try
            {
                return Injector?.GetService(type) ?? Activator.CreateInstance(type);
            }
            catch
            {
                return null;
            }
        }

        public static object GetRegistered(Type type) => Injector == null
            ? throw new NullReferenceException($"App must set an {nameof(Injector)} that implements {nameof(IServiceProvider)}")
            : Injector.GetRequiredService(type);

        public static async void Update(IServiceProvider newInjector)
        {
            var oldInjector = Injector;
            Injector = newInjector;

            switch (oldInjector)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    break;

                case IDisposable disposable:
                    await Task.Run(disposable.Dispose).ConfigureAwait(false);
                    break;
            }
        }

        public object? GetService(Type serviceType) => Get(serviceType);
    }
}
