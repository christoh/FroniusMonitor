using System;
using System.IO;

namespace De.Hochstaetter.HomeAutomationClient.Desktop.Platform;

/// <summary>
/// Windows, macOS and Linux all have a local application data directory of their own, and .NET knows where it is.
/// </summary>
public class Cache() : FileCache(DataDirectory)
{
    private static string DataDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Hochstätter", "HomeAutomationClient");
}
