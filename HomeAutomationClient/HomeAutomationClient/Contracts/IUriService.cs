namespace De.Hochstaetter.HomeAutomationClient.Contracts;

/// <summary>
/// The address of the view that is currently shown. In the browser this is the address bar; the other heads have
/// no address bar and remember the addresses instead.
/// </summary>
public interface IUriService
{
    /// <summary>
    /// The path the app was started with, so that a link into a detail view still leads there after the login.
    /// <see cref="ViewPath.Dashboard"/> where the platform has no address to start from.
    /// </summary>
    string StartupPath { get; }

    /// <summary>
    /// The user asked for another address without the app doing it - the back and forward buttons of the
    /// browser. The address has already changed when this is raised; only the view has to follow, and nothing
    /// must be written back into the history. Heads without an address bar never raise it.
    /// </summary>
    event EventHandler<string>? PathChanged;

    /// <summary>
    /// Makes <paramref name="path"/> the address of the app. Never reloads anything, and does nothing when it is
    /// the address already - navigating to the view that is shown must not add a second history entry.
    /// </summary>
    void SetPath(string path);
}
