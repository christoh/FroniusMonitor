using System.Collections.ObjectModel;

namespace De.Hochstaetter.HomeAutomationClient.Services;

/// <summary>
/// The address for every head that has no address bar: desktop, Android and iOS. Nothing shows these paths yet,
/// they are collected so that whoever needs them next - a back button, restoring the last view on start, a link
/// to share - finds a history that is already correct.
/// </summary>
public sealed class FakeUriService : IUriService
{
    /// <summary>
    /// Every address the app has shown, oldest first, without scheme and host: "/" or
    /// "/inverterdetails/Fronius/12345678". The current one is the last entry.
    /// </summary>
    public ObservableCollection<string> Uris { get; } = [];

    /// <summary>
    /// There is no address to start from outside the browser, so the app opens where it always did.
    /// </summary>
    public string StartupPath => ViewPath.Dashboard;

    public void SetPath(string path)
    {
        if (Uris.Count == 0 || !string.Equals(Uris[^1], path, StringComparison.Ordinal))
        {
            Uris.Add(path);
        }
    }
}
