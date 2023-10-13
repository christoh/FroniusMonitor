namespace De.Hochstaetter.Fronius.Models.Settings;

public abstract class SettingsBase : BindableBase, ICloneable
{
    public event EventHandler<EventArgs>? SettingsChanged;

    private static readonly SemaphoreSlim settingLock = new(1, 1);

    protected static T Load<T>(string fileName) where T : SettingsBase, new()
    {
        settingLock.Wait();

        try
        {
            using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            var serializer = new XmlSerializer(typeof(T));
            var settings = serializer.Deserialize(stream) as T ?? throw new SerializationException();
            ClearIncorrectPasswords(CollectWebConnections(settings).ToArray());
            return settings;
        }
        finally
        {
            settingLock.Release();
        }
    }

    protected static void Save<T>(T settings, string fileName) where T : SettingsBase, new()
    {
        settingLock.Wait();

        try
        {
            UpdateChecksum(CollectWebConnections(settings).ToArray());
            var serializer = new XmlSerializer(typeof(T));
            using var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);

            using var writer = XmlWriter.Create(stream, new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                IndentChars = new string(' ', 3),
                NewLineChars = Environment.NewLine,
            });

            serializer.Serialize(writer, settings);
        }
        finally
        {
            settingLock.Release();
        }
    }

    protected static IEnumerable<WebConnection?> CollectWebConnections<T>(T instance)
    {
        var connectionProperties = typeof(T)
            .GetProperties()
            .Where(prop => typeof(WebConnection).IsAssignableFrom(prop.PropertyType))
            .Select(prop => (WebConnection?)prop.GetValue(instance));

        var collectionProperties = typeof(T)
            .GetProperties()
            .Where(prop => typeof(IEnumerable<WebConnection>).IsAssignableFrom(prop.PropertyType))
            .SelectMany(prop =>
            {
                if (prop.GetValue(instance) is IEnumerable<WebConnection> collection)
                {
                    return collection.Select(connection => (WebConnection?)connection);
                }
                return Enumerable.Empty<WebConnection?>();
            });

        return connectionProperties.Concat(collectionProperties);
    }

    protected static void UpdateChecksum(params WebConnection?[] connections)
    {
        connections.Where(connection => connection != null).Apply(connection => { connection!.PasswordChecksum = connection.CalculatedChecksum; });
    }

    protected static void ClearIncorrectPasswords(params WebConnection?[] connections)
    {
        connections.Where(connection => connection != null && connection.PasswordChecksum != connection.CalculatedChecksum).Apply(connection => connection!.Password = string.Empty);
    }

    public object Clone()
    {
        var clone = (SettingsBase)MemberwiseClone();

        foreach (var propertyInfo in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).Where(p => p is { CanRead: true, CanWrite: true } && p.PropertyType.GetInterface(nameof(ICloneable)) is not null))
        {
            var value = propertyInfo.GetValue(clone);

            if (value != null)
            {
                var cloneMethod = propertyInfo.PropertyType.GetMethod(nameof(ICloneable.Clone));
                propertyInfo.SetValue(clone, cloneMethod?.Invoke(value, null));
            }
        }

        return clone;
    }

    public void CopyFrom(SettingsBase other)
    {
        var otherType = other.GetType();

        if (!GetType().IsAssignableFrom(otherType))
        {
            throw new ArgumentException($"{GetType().Name} is not assignable from {otherType}");
        }

        foreach (var propertyInfo in other.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).Where(p => p is { CanRead: true, CanWrite: true }))
        {
            var value = propertyInfo.GetValue(other);

            if (value is null || propertyInfo.PropertyType.GetInterface(nameof(ICloneable)) == null)
            {
                propertyInfo.SetValue(this, value);
            }
            else
            {
                var cloneMethod = propertyInfo.PropertyType.GetMethod(nameof(ICloneable.Clone));
                propertyInfo.SetValue(this, cloneMethod?.Invoke(value, null));
            }
        }
    }

    public void NotifySettingsChanged() => SettingsChanged?.Invoke(this, EventArgs.Empty);
}
