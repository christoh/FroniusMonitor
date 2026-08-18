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

    /// <summary>
    /// <para>Change the value of a property with notification via <see cref="INotifyPropertyChanged"/>. Typically used in the setter of a property that cannot be handled by <see cref="ObservablePropertyAttribute"/>.</para>
    /// <remarks>This method provides more functionality than <see cref="ObservableObject.SetProperty{T}(ref T, T, string?)"/> but consumes slightly more CPU time.</remarks>
    /// </summary>
    /// <param name="backingField">The backing field of the property. For auto-properties use the keyword <see langword="field"/>.</param>
    /// <param name="value">The new value the property should get</param>
    /// <param name="postAction">
    /// <para>An optional <see cref="Action"/> that is performed after the property has changed, e.g. notify other properties.</para>
    /// <para>This <see cref="Action"/>> will not be performed if the property has not changed and <paramref name="notifyAlways"/> is <see langword="false"/>.</para>
    /// </param>
    /// <param name="preFunc">
    /// <para>An optional <see cref="Func{T}"/> that can be used for validation or coercion (e.g. coerce between 0 and 100%).</para>
    /// <para>The return value of this <see cref="Func{T}"/> overrides the <paramref name="value"/>.</para>
    /// <para>If you wish no change, simply return <paramref name="value"/>.</para>
    /// <para>This <see cref="Func{T}"/>> will not be called if the property has not changed and <paramref name="notifyAlways"/> is <see langword="false"/>.</para>
    /// </param>
    /// <param name="propertyName">The name of the property. Normally leave it to <see langword="null"/>. This will set the property name automatically using <see cref="CallerMemberNameAttribute"/>.</param>
    /// <param name="notifyAlways"><see langword="true"/> if you want to notify and perform <paramref name="preFunc"/> and <paramref name="postAction"/> even if the property has not changed.</param>
    /// <param name="comparer">An optional <see cref="IEqualityComparer{T}"/> to test if the property changes to a new value.</param>
    /// <returns><see langword="true"/> if the property has changed, or <see langword="false"/> if the new value is the same as the previous one.</returns>
    [RequiresUnreferencedCode("The type of the current instance cannot be statically discovered.")]
    protected virtual bool SetProperty<T>(ref T backingField, T value, Action? postAction = null, Func<T>? preFunc = null, [CallerMemberName] string? propertyName = null, bool notifyAlways = false, IEqualityComparer<T>? comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;
        var hasChanged = !comparer.Equals(backingField, value);

        if (!hasChanged && !notifyAlways)
        {
            return false;
        }

        if (preFunc != null)
        {
            value = preFunc.Invoke();
            hasChanged = !comparer.Equals(backingField, value);
        }

        if (!hasChanged && !notifyAlways)
        {
            return false;
        }

        OnPropertyChanging(propertyName);
        backingField = value;
        postAction?.Invoke();
        OnPropertyChanged(propertyName);
        return hasChanged;
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

    /// <summary>
    /// Triggers a refresh of the object's state by raising the <see cref="INotifyPropertyChanged.PropertyChanged"/> event for all properties.<br/>
    /// It Refreshes all properties even if <see cref="IsNotifying"/> is <see langword="false"/>.
    /// </summary>
    /// <param name="enableNotifying">
    /// A boolean value indicating whether to re-enable property change notifications after the refresh.
    /// Defaults to <see langword="false"/>.
    /// </param>
    public virtual void Refresh(bool enableNotifying = false)
    {
        var previousState = IsNotifying;
        IsNotifying = true;
        OnPropertyChanged(string.Empty);
        IsNotifying = enableNotifying || previousState;
    }
}
