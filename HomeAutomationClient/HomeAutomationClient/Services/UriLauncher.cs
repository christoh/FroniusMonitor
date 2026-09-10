using Avalonia.Controls;

namespace De.Hochstaetter.HomeAutomationClient.Services;

/// <summary>
/// <see cref="IUriLauncher"/> through the <see cref="TopLevel"/> of the main view, which is where Avalonia hangs the
/// platform's launcher: a window on the desktop, the page in the browser, the activity on Android.
/// </summary>
internal sealed class UriLauncher(MainView mainView) : IUriLauncher
{
    public Task<bool> LaunchAsync(Uri uri) => TopLevel.GetTopLevel(mainView)?.Launcher.LaunchUriAsync(uri) ?? Task.FromResult(false);
}
