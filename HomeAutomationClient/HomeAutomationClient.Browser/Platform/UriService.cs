using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.HomeAutomationClient.Misc;

namespace De.Hochstaetter.HomeAutomationClient.Browser.Platform;

/// <summary>
/// The address bar of the browser, read once at startup and written on every navigation.
/// </summary>
public sealed partial class UriService : IUriService
{
    [JSImport("getPath", "uri")]
    private static partial string GetPath();

    [JSImport("pushPath", "uri")]
    private static partial void PushPath(string path);

    private UriService(string startupPath) => StartupPath = startupPath;

    public string StartupPath { get; }

    /// <summary>
    /// Imports the module and remembers the address the user arrived with. A browser that refuses the module
    /// leaves the app without an address bar rather than without an app.
    /// </summary>
    public static async Task<UriService> CreateAsync()
    {
        try
        {
            // The module URL is resolved relative to the .NET runtime in _framework, not to the document.
            await JSHost.ImportAsync("uri", "../uri.js");
            return new UriService(GetPath());
        }
        catch (Exception exception)
        {
            Console.WriteLine("Could not read the address of the browser: " + exception.Message);
            return new UriService(ViewPath.Dashboard);
        }
    }

    public void SetPath(string path)
    {
        try
        {
            if (!string.Equals(GetPath(), path, StringComparison.Ordinal))
            {
                PushPath(path);
            }
        }
        catch (Exception exception)
        {
            // The view is shown either way; only the address bar stays behind.
            Console.WriteLine("Could not set the address of the browser: " + exception.Message);
        }
    }
}
