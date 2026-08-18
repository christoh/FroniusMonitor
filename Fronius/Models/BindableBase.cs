using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace De.Hochstaetter.Fronius.Models;

public abstract class BindableBase : ObservableValidator
{
    [Flags]
    private enum Flags : byte
    {
        None = 0,
        IsNotifying = 1 << 0,
        IsNotifyingBeforeChanging = 1 << 1,
    }

    private Flags flags = Flags.IsNotifying;

    /// <summary>
    /// Gets or sets a value indicating whether property change notifications are enabled.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, the <see cref="OnPropertyChanged(PropertyChangedEventArgs)"/> method will notify listeners of property changes.
    /// Set this to <c>false</c> to suppress notifications, which can be useful for batch updates or performance optimization.<br/>
    /// </remarks>
    [NotMapped, IgnoreDataMember, JsonIgnore, XmlIgnore, SoapIgnore, ContractRuntimeIgnored]
    public virtual bool IsNotifying
    {
        get => (flags & Flags.IsNotifying) != Flags.None;
        set => flags = (value ? flags | Flags.IsNotifying : flags & ~Flags.IsNotifying);
    }

    /// <summary>
    /// Gets or sets a value indicating whether property change notifications are sent before the property value is changed.
    /// This defaults to <see langword="false"/>. When set to <see langword="true"/>, the <see cref="OnPropertyChanging(PropertyChangingEventArgs)"/> method will notify listeners before the property value is changed.
    /// </summary>
    [NotMapped, IgnoreDataMember, JsonIgnore, XmlIgnore, SoapIgnore, ContractRuntimeIgnored]
    public virtual bool IsNotifyingBeforeChanging
    {
        get => (flags & Flags.IsNotifyingBeforeChanging) != Flags.None;
        set => flags = (value ? flags | Flags.IsNotifyingBeforeChanging : flags & ~Flags.IsNotifyingBeforeChanging);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (IsNotifying)
        {
            base.OnPropertyChanged(e);
        }
    }

    /// <inheritdoc />
    protected override void OnPropertyChanging(PropertyChangingEventArgs e)
    {
        if (IsNotifyingBeforeChanging)
        {
            base.OnPropertyChanging(e);
        }
    }


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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual bool Set<T>(ref T backingField, T value, Action? postAction = null, Func<T>? preFunc = null, [CallerMemberName] string? propertyName = null, bool notifyAlways = false)
    {
        return SetProperty(ref backingField, value, postAction, preFunc, propertyName);
    }

    // Same as OnPropertyChanged (for compatibility with Caliburn.Micro)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void NotifyOfPropertyChange([CallerMemberName] string? propertyName = null) => OnPropertyChanged(propertyName);

    public virtual void Refresh(bool setIsNotifying=true)
    {
        if (setIsNotifying)
        {
            IsNotifying = true;
        }
        
        NotifyOfPropertyChange(string.Empty);
    }
}
