using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace De.Hochstaetter.HomeAutomationClient.Misc;

/// <summary>
/// Keeps a <see cref="INotifyCollectionChanged.CollectionChanged"/> handler on the collection a property currently
/// answers, and moves it when the property is found to answer another one.
/// </summary>
/// <remarks>
/// The update service does not only add to its collections, it replaces them: <c>Inverters</c> whenever an inverter
/// appears, both of them at logout. A handler put on the collection once, at construction, is left on the old one
/// and hears nothing more - a singleton such as <c>HouseViewModel</c> would go on following the Wattpilots of the
/// session before. So a view model keeps the instance it follows in a field and calls <see cref="Follow{T}"/> each
/// time the service announces the property, which is a no-op while the instance is the same.
/// </remarks>
internal static class CollectionFollowing
{
    public static void Follow<T>(ref ObservableCollection<T>? followed, ObservableCollection<T> current, NotifyCollectionChangedEventHandler handler)
    {
        if (ReferenceEquals(followed, current))
        {
            return;
        }

        if (followed != null)
        {
            followed.CollectionChanged -= handler;
        }

        followed = current;
        current.CollectionChanged += handler;
    }

    public static void Unfollow<T>(ref ObservableCollection<T>? followed, NotifyCollectionChangedEventHandler handler)
    {
        if (followed == null)
        {
            return;
        }

        followed.CollectionChanged -= handler;
        followed = null;
    }
}
