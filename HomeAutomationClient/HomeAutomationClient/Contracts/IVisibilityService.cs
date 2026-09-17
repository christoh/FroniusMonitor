namespace De.Hochstaetter.HomeAutomationClient.Contracts;

/// <summary>
/// Whether the user can see anything that shows live data. Nothing here is a UI type, so a view model and the
/// update service may ask; how the answer is found - window states on the desktop, the tab and the app
/// lifecycle elsewhere - is <c>Services/VisibilityService</c>'s.
/// </summary>
/// <remarks>
/// <para>
/// A minimized or hidden window cannot be seen. An app the platform has put into the background cannot be seen
/// either, whatever its windows say: a browser tab that is not the current one, a phone app behind another, a
/// macOS app the user has hidden. A window that is merely behind another window, or on a virtual desktop the user
/// is not looking at, counts as visible - no platform Avalonia runs on tells it about either.
/// </para>
/// <para>
/// The answer may flip on the UI thread while <see cref="IsAnyVisible"/> is read on the hub's, so it is one
/// boolean, read whole, and <see cref="IsAnyVisibleChanged"/> is raised on the UI thread.
/// </para>
/// </remarks>
public interface IVisibilityService
{
    /// <summary>
    /// True while at least one window of the app can be seen by the user. Where there is nothing to judge by - no
    /// window at all, a platform without a lifecycle - it stays true: better a connection nobody looks at than a
    /// dashboard that does not move.
    /// </summary>
    bool IsAnyVisible { get; }

    /// <summary>Raised whenever <see cref="IsAnyVisible"/> changes, and only then.</summary>
    event EventHandler? IsAnyVisibleChanged;
}
