﻿namespace De.Hochstaetter.Fronius.Models;

public abstract record BindableRecordBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

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

        backingField = value;
        postAction?.Invoke();
        RaisePropertyChanged(propertyName);
        return true;
    }

    // Same as SetProperty (for compatibility with Caliburn.Micro)
    protected virtual bool Set<T>(ref T backingField, T value, Action? postAction = null, Func<T>? preFunc = null, [CallerMemberName] string? propertyName = null, bool notifyAlways = false)
    {
        return SetProperty(ref backingField, value, postAction, preFunc, propertyName);
    }

    public virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null) => OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

    // Same as RaisePropertyChanged (for compatibility with Caliburn.Micro)
    public virtual void NotifyOfPropertyChange([CallerMemberName] string? propertyName = null) => RaisePropertyChanged(propertyName);

    public virtual void Refresh() => NotifyOfPropertyChange(string.Empty);

    protected virtual void OnPropertyChanged(PropertyChangedEventArgs args) => PropertyChanged?.Invoke(this, args);
}
