using System;
using De.Hochstaetter.HomeAutomationClient;

namespace HomeAutomationClient.Android.Platform;

/// <summary>
/// The files directory of the app is its private data directory. Application.Context exists from OnCreate on,
/// which is where the head creates this cache.
/// </summary>
public class Cache() : FileCache(DataDirectory)
{
    private static string DataDirectory => global::Android.App.Application.Context.FilesDir?.AbsolutePath ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
}
