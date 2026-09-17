namespace De.Hochstaetter.HomeAutomationClient.Controls;

/// <summary>
/// Tells a view model whether the user can see its view. A view that follows the update service - the power flow
/// page, the house block, the price chart - calls <see cref="Follow"/> once from its constructor, and from then on
/// the <see cref="ViewModelBase.IsShown"/> of whatever is its <c>DataContext</c> says whether the view is in the
/// visual tree of a window the user can see. The view model decides what to skip while it is not; see
/// <see cref="ViewModelBase.WhenShown"/>.
/// </summary>
/// <remarks>
/// This is the marshalling the interaction rule leaves to the code behind: it needs the window and the visual
/// tree, and it hands the view model one boolean. Nothing is decided here. Without a
/// <see cref="VisibilityService"/> in the container - the designer, a test that did not register one - nothing is
/// followed and the view model keeps its default, which is shown.
/// </remarks>
public static class ViewVisibility
{
    public static void Follow(Control control)
    {
        if (IoC.TryGetRegistered<VisibilityService>() is not { } service)
        {
            return;
        }

        void Tell(bool isShown)
        {
            if (control.DataContext is ViewModelBase viewModel)
            {
                viewModel.IsShown = isShown;
            }
        }

        void Apply(object? sender, EventArgs e) => Tell(IsShown(control, service));

        control.AttachedToVisualTree += (_, _) =>
        {
            service.VisibilityChanged += Apply;
            Apply(null, EventArgs.Empty);
        };

        // Not shown, said outright: whether the root is still reachable while the event runs is Avalonia's
        // business, and a detached control is not seen either way.
        control.DetachedFromVisualTree += (_, _) =>
        {
            service.VisibilityChanged -= Apply;
            Tell(false);
        };

        // A view model that arrives after the view is up has to be told where it is; one that leaves is left as it was.
        control.DataContextChanged += Apply;
    }

    /// <summary>
    /// Whether the user can see <paramref name="control"/>: it is attached to a top level, and that top level is
    /// one the user can see. Detached is not seen, whatever the window does - a page the browser head has
    /// navigated away from is detached and nothing else.
    /// </summary>
    public static bool IsShown(Control control) => IoC.TryGetRegistered<VisibilityService>() is not { } service || IsShown(control, service);

    private static bool IsShown(Control control, VisibilityService service) => TopLevel.GetTopLevel(control) is { } topLevel && service.IsVisible(topLevel);
}
