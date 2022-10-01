namespace De.Hochstaetter.FroniusMonitor.Extensions;

public static class DependencyObjectExtensions
{
    public static IEnumerable<TDependencyObject> FindVisualChildren<TDependencyObject>(this DependencyObject d) where TDependencyObject : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
        {
            var child = VisualTreeHelper.GetChild(d, i);

            if (child is TDependencyObject dependencyObject)
            {
                yield return dependencyObject;
            }

            foreach (var childOfChild in child.FindVisualChildren<TDependencyObject>())
            {
                yield return childOfChild;
            }
        }
    }

    public static T? GetVisualParent<T>(this DependencyObject? element) where T : DependencyObject
    {
        while (true)
        {
            if (element is null)
            {
                return null;
            }

            element = VisualTreeHelper.GetParent(element);

            if (element is T desiredElement)
            {
                return desiredElement;
            }
        }
    }
}