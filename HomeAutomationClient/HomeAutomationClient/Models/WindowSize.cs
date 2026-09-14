namespace De.Hochstaetter.HomeAutomationClient.Models;

/// <summary>
/// What <see cref="Misc.StoredWindowSize"/> keeps of the desktop window between two runs: its client size in
/// device independent pixels, and whether it was maximized. Deliberately no position - see there.
/// </summary>
/// <remarks>
/// Plain settable properties, because the cache drops every get-only one (<see cref="CacheJson"/>).
/// </remarks>
public sealed class WindowSize
{
    public double Width { get; set; }
    public double Height { get; set; }
    public bool IsMaximized { get; set; }
}
