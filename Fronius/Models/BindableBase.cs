using CommunityToolkit.Mvvm.ComponentModel;

namespace De.Hochstaetter.Fronius.Models;

public abstract class BindableBase : ObservableObject
{
    protected virtual bool SetProperty<T>(ref T backingField, T value, Action? postAction = null, Func<T>? preFunc = null, [CallerMemberName] string? propertyName = null, bool notifyAlways = false)
    {
        if (!notifyAlways && EqualityComparer<T>.Default.Equals(backingField, value))
        {
            return false;
        }

        if (preFunc != null)
        {
            value = preFunc.Invoke();
        }

        OnPropertyChanging(propertyName);
        backingField = value;
        postAction?.Invoke();
        NotifyOfPropertyChange(propertyName);
        return true;
    }

    // Same as SetProperty (for compatibility with Caliburn.Micro)
    protected virtual bool Set<T>(ref T backingField, T value, Action? postAction = null, Func<T>? preFunc = null, [CallerMemberName] string? propertyName = null, bool notifyAlways = false)
    {
        return SetProperty(ref backingField, value, postAction, preFunc, propertyName);
    }

    // Same as RaisePropertyChanged (for compatibility with Caliburn.Micro)
    public virtual void NotifyOfPropertyChange([CallerMemberName] string? propertyName = null) => OnPropertyChanged(propertyName);

    public virtual void Refresh() => NotifyOfPropertyChange(string.Empty);
}
