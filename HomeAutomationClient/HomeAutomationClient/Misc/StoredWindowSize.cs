namespace De.Hochstaetter.HomeAutomationClient.Misc;

/// <summary>
/// The size the desktop window had when it was last closed, so that the next start opens it that big again.
/// </summary>
/// <remarks>
/// <para>
/// Only the size. Where the window stood is deliberately not kept: the window opens where the OS puts it, which
/// also spares it from coming back on a monitor that has since been unplugged. Desktop only, because the desktop
/// lifetime alone has a window - the other heads fill their screen - so the one caller is
/// <see cref="Views.MainWindow"/>.
/// </para>
/// <para>
/// This is the decision part - what counts as a usable size, how a maximized window is remembered - kept free of
/// Avalonia types like <see cref="StoredConnection"/> is, so that a unit test can drive it with a
/// <see cref="FileCache"/>. The code behind only reads and writes the window's own properties.
/// </para>
/// </remarks>
public static class StoredWindowSize
{
    /// <summary>
    /// Smaller than this and not even the menu bar fits. A stored size below it is treated as not stored, which
    /// also covers a hand edited cache file.
    /// </summary>
    public const double MinimumWidth = 320;

    public const double MinimumHeight = 240;

    /// <summary>
    /// The stored size, cut down to the working area it has to fit into, or <see langword="null"/> when nothing
    /// usable is stored. A maximum of zero or less means "no limit".
    /// </summary>
    public static WindowSize? Load(ICache? cache, double maximumWidth = 0, double maximumHeight = 0)
    {
        if (cache?.Get<WindowSize>(CacheKeys.WindowSize) is not { } stored || !IsUsable(stored.Width, stored.Height))
        {
            return null;
        }

        return new WindowSize
        {
            Width = maximumWidth > 0 ? Math.Min(stored.Width, maximumWidth) : stored.Width,
            Height = maximumHeight > 0 ? Math.Min(stored.Height, maximumHeight) : stored.Height,
            IsMaximized = stored.IsMaximized,
        };
    }

    /// <summary>
    /// Called when the window closes with the client size it has at that moment. A maximized window keeps the
    /// size that is already stored - the one it had before it was maximized - and only notes that it was
    /// maximized, so that it comes back maximized and leaving that state gives the old size rather than a window
    /// as big as the screen. Nothing is written for a size that <see cref="Load"/> would throw away anyway.
    /// </summary>
    public static void Save(ICache? cache, double width, double height, bool isMaximized)
    {
        if (cache == null)
        {
            return;
        }

        var previous = isMaximized ? Load(cache) : null;
        var (storedWidth, storedHeight) = previous is { } ? (previous.Width, previous.Height) : (width, height);

        if (!IsUsable(storedWidth, storedHeight))
        {
            return;
        }

        cache.AddOrUpdate(CacheKeys.WindowSize, new WindowSize { Width = storedWidth, Height = storedHeight, IsMaximized = isMaximized });
    }

    // A NaN fails the comparison on its own; infinity would pass it.
    private static bool IsUsable(double width, double height) => double.IsFinite(width) && double.IsFinite(height) && width >= MinimumWidth && height >= MinimumHeight;
}
