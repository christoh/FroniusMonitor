using De.Hochstaetter.HomeAutomationClient;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

/// <summary>
/// The client's <see cref="FileCache"/> in a directory of its own, which <see cref="Dispose"/> removes again.
/// </summary>
internal sealed class TempFileCache : FileCache, IDisposable
{
    public string Directory { get; }

    public TempFileCache() : this(Path.Combine(Path.GetTempPath(), $"FroniusMonitorCacheTests-{Guid.NewGuid():N}")) { }

    private TempFileCache(string directory) : base(directory)
    {
        Directory = directory;
    }

    public void Dispose()
    {
        if (System.IO.Directory.Exists(Directory))
        {
            System.IO.Directory.Delete(Directory, true);
        }
    }
}
