using System;
using System.IO;

namespace De.Hochstaetter.HomeAutomationClient.Desktop.Platform;

/// <summary>
/// Windows, macOS and Linux all have a local application data directory of their own, and .NET knows where it is.
/// </summary>
public class Cache() : FileCache(DataDirectory)
{
    /// <summary>
    /// Internal rather than private because the log file of this head belongs next to the cache, and the path is
    /// worth having in one place. It is the same convention FroniusMonitor uses for its own per user data.
    /// </summary>
    internal static string DataDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Hochstätter", "HomeAutomationClient");
}
