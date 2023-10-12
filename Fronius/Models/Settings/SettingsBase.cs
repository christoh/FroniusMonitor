namespace De.Hochstaetter.Fronius.Models.Settings;

public abstract class SettingsBase : BindableBase, ICloneable
{
    public event EventHandler<EventArgs>? SettingsChanged;

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
