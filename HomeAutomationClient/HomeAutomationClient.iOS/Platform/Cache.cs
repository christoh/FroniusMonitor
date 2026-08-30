using System;
using System.Linq;
using De.Hochstaetter.HomeAutomationClient;
using Foundation;

namespace HomeAutomationClient.iOS.Platform;

/// <summary>
/// Application Support is the data directory of an iOS app. Unlike the other directories it does not exist until
/// somebody creates it, which <see cref="FileCache"/> does.
/// </summary>
public class Cache() : FileCache(DataDirectory)
{
    private static string DataDirectory =>
        NSFileManager.DefaultManager.GetUrls(NSSearchPathDirectory.ApplicationSupportDirectory, NSSearchPathDomain.User).FirstOrDefault()?.Path
        ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
}
