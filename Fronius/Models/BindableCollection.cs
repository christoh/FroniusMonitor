namespace De.Hochstaetter.Fronius.Models;

public class BindableCollection<T> : ObservableCollection<T>, ICloneable
{
    public BindableCollection(SynchronizationContext? context = null)
    {
        Context = context ?? SynchronizationContext.Current ?? throw new ThreadStateException("No context");
    }

    public BindableCollection(IEnumerable<T> collection, SynchronizationContext? context = null) : base(collection)
    {
        Context = context ?? SynchronizationContext.Current ?? throw new ThreadStateException("No context");
    }

    public SynchronizationContext Context { get; }

    public bool IsNotifying { get; set; } = true;

    public virtual void NotifyOfPropertyChange(string propertyName)
    {
        if (IsNotifying)
        {
            OnUIThread(() => OnPropertyChanged(new PropertyChangedEventArgs(propertyName)));
        }
    }

    public void Refresh()
    {
        OnUIThread(() =>
        {
            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        });
    }

    protected override void InsertItem(int index, T item)
    {
        OnUIThread(() => base.InsertItem(index, item));
    }

    protected override void SetItem(int index, T item)
    {
        OnUIThread(() => base.SetItem(index, item));
    }

    protected sealed override void RemoveItem(int index)
    {
        OnUIThread(() => base.RemoveItem(index));
    }

    protected sealed override void ClearItems()
    {
        OnUIThread(base.ClearItems);
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (IsNotifying)
        {
            base.OnCollectionChanged(e);
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (IsNotifying)
        {
            base.OnPropertyChanged(e);
        }
    }

    public virtual void AddRange(IEnumerable<T> items)
    {
        OnUIThread(() =>
        {
            var previousNotificationSetting = IsNotifying;
            IsNotifying = false;
            var index = Count;

            foreach (var item in items)
            {
                base.InsertItem(index, item);
                index++;
            }

            IsNotifying = previousNotificationSetting;

            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        });
    }

    public virtual void RemoveRange(IEnumerable<T> items)
    {
        OnUIThread(() =>
        {
            var previousNotificationSetting = IsNotifying;
            IsNotifying = false;

            foreach (var item in items)
            {
                var index = IndexOf(item);

                if (index >= 0)
                {
                    base.RemoveItem(index);
                }
            }

            IsNotifying = previousNotificationSetting;

            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        });
    }

    protected virtual void OnUIThread(Action action)
    {
        if (SynchronizationContext.Current == Context)
        {
            action();
            return;
        }

        Context.Send(_ => action(), null);
    }

    public object Clone()
    {
        return new BindableCollection<T>(Items.Select(i => i is ICloneable cloneable ? (T)cloneable.Clone() : i), Context) { IsNotifying = IsNotifying };
    }
}