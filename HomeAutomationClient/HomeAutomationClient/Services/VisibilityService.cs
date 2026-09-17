namespace De.Hochstaetter.HomeAutomationClient.Services;

/// <summary>
/// The one place that knows whether the user can see the app: every window's state on the desktop, and the
/// platform's word on whether the app is in the background everywhere it gives one. <see cref="IVisibilityService"/>
/// is the framework free half of it, for the update service; the views ask <see cref="IsVisible"/> about their own
/// window through <c>Controls/ViewVisibility</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Windows report themselves.</b> Nothing registers a window here: the class handlers in the static constructor
/// see every window's <see cref="Window.WindowState"/> and <see cref="Visual.IsVisible"/> change, and the desktop
/// lifetime's <see cref="IClassicDesktopStyleApplicationLifetime.Windows"/> is the list that is judged. So a
/// message box counts as much as the main window, and nothing has to remember to sign a new kind of window up.
/// A window is seen when it is shown and not minimized. Being behind another window is being seen, and so is
/// being on another virtual desktop: no platform Avalonia runs on reports either, so nothing here can either.
/// </para>
/// <para>
/// <b>The platform says when the whole app is out of sight.</b> <see cref="IActivatableLifetime"/> raises
/// <see cref="ActivationKind.Background"/> where the platform has the notion: the browser head when the tab is
/// hidden or the window minimized - Avalonia listens to <c>document.visibilitychange</c> itself, so no script of
/// ours is needed - the phones when the app goes behind another, macOS when the app is hidden. Windows and Linux
/// have no such notion, and there the windows alone decide.
/// </para>
/// <para>
/// Everything is on the UI thread, where Avalonia raises its events. <see cref="IsAnyVisible"/> is one field for
/// the hub's thread to read.
/// </para>
/// </remarks>
public sealed class VisibilityService : IVisibilityService, IDisposable
{
    /// <summary>Every window that is being followed for its <see cref="Window.Closed"/>, so none is followed twice.</summary>
    private static readonly HashSet<Window> followedWindows = [];

    /// <summary>Raised on the UI thread whenever any window is shown, hidden, closed, minimized or restored.</summary>
    private static event EventHandler? WindowsChanged;

    private readonly IActivatableLifetime? activatable;
    private bool isInBackground;
    private volatile bool isAnyVisible = true;

    static VisibilityService()
    {
        Window.WindowStateProperty.Changed.AddClassHandler<Window>((_, _) => WindowsChanged?.Invoke(null, EventArgs.Empty));
        Visual.IsVisibleProperty.Changed.AddClassHandler<Window>(OnWindowIsVisibleChanged);
    }

    public VisibilityService()
    {
        // A lifetime that has the notion of a background: the browser, the phones, macOS. TryGetFeature answers
        // null where the lifetime does not implement it, which is the plain desktop.
        activatable = Application.Current?.TryGetFeature(typeof(IActivatableLifetime)) as IActivatableLifetime;

        if (activatable != null)
        {
            activatable.Activated += OnActivated;
            activatable.Deactivated += OnDeactivated;
        }

        WindowsChanged += OnWindowsChanged;
        Recompute();
    }

    public bool IsAnyVisible => isAnyVisible;

    public event EventHandler? IsAnyVisibleChanged;

    /// <summary>
    /// Raised on the UI thread whenever anything that <see cref="IsVisible"/> answers from has changed. A view
    /// that follows its own window listens here and asks again; it is coarser than <see cref="IsAnyVisibleChanged"/>,
    /// which only says when the whole app changes between seen and not seen.
    /// </summary>
    public event EventHandler? VisibilityChanged;

    /// <summary>
    /// Whether the user can see what is in <paramref name="topLevel"/>: the app is not in the background, and
    /// where the top level is a window, that window is shown and not minimized. Anything that is not a window - the
    /// one view of the browser and the phones, a popup - is seen whenever the app is.
    /// </summary>
    public bool IsVisible(TopLevel? topLevel) => !isInBackground && (topLevel is not Window window || CanBeSeen(window));

    public void Dispose()
    {
        WindowsChanged -= OnWindowsChanged;

        if (activatable != null)
        {
            activatable.Activated -= OnActivated;
            activatable.Deactivated -= OnDeactivated;
        }
    }

    private static bool CanBeSeen(Window window) => window.IsVisible && window.WindowState != WindowState.Minimized;

    /// <summary>
    /// A window that becomes visible is shown for the first time or again, so its <see cref="Window.Closed"/> is
    /// followed from here on: the lifetime takes a closed window off its list without a property of the window
    /// changing, and the list is what is judged.
    /// </summary>
    private static void OnWindowIsVisibleChanged(Window window, AvaloniaPropertyChangedEventArgs e)
    {
        if (followedWindows.Add(window))
        {
            window.Closed += OnWindowClosed;
        }

        WindowsChanged?.Invoke(null, EventArgs.Empty);
    }

    private static void OnWindowClosed(object? sender, EventArgs e)
    {
        if (sender is Window window)
        {
            window.Closed -= OnWindowClosed;
            followedWindows.Remove(window);
        }

        WindowsChanged?.Invoke(null, EventArgs.Empty);

        // Closed is raised while the window is still on the lifetime's list; it is taken off afterwards. A second
        // look once the dispatcher comes round sees the list as it is then - which matters when it is empty.
        Dispatcher.UIThread.Post(() => WindowsChanged?.Invoke(null, EventArgs.Empty));
    }

    private void OnWindowsChanged(object? sender, EventArgs e) => Recompute();

    private void OnActivated(object? sender, ActivatedEventArgs e)
    {
        if (e.Kind == ActivationKind.Background)
        {
            isInBackground = false;
            Recompute();
        }
    }

    private void OnDeactivated(object? sender, ActivatedEventArgs e)
    {
        if (e.Kind == ActivationKind.Background)
        {
            isInBackground = true;
            Recompute();
        }
    }

    /// <summary>
    /// Works <see cref="IsAnyVisible"/> out anew and says so if it changed. Where there is no list of windows -
    /// the single view heads - or the list is empty, the windows say nothing against being seen: better a
    /// connection nobody looks at than a dashboard that does not move.
    /// </summary>
    private void Recompute()
    {
        var windows = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.Windows : null;
        var visible = !isInBackground && (windows is not { Count: > 0 } || windows.Any(CanBeSeen));

        VisibilityChanged?.Invoke(this, EventArgs.Empty);

        if (visible == isAnyVisible)
        {
            return;
        }

        isAnyVisible = visible;
        IsAnyVisibleChanged?.Invoke(this, EventArgs.Empty);
    }
}
