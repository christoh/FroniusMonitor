namespace De.Hochstaetter.HomeAutomationClient.Contracts;

/// <summary>
/// Opens a link outside the app - a download, a document - wherever the app happens to run: the default browser on
/// the desktop, a new tab in the browser head, the system handler on a phone.
/// </summary>
/// <remarks>
/// A view model may not reach for <c>Process.Start</c>: it does not exist on WebAssembly, and on iOS and Android it
/// is not what opens a link either. Avalonia's <c>ILauncher</c> is, on every head, and this is the view model's way
/// to it without knowing Avalonia.
/// </remarks>
public interface IUriLauncher
{
    /// <summary>Whether something took the link. False where nothing on this platform could open it.</summary>
    Task<bool> LaunchAsync(Uri uri);
}
