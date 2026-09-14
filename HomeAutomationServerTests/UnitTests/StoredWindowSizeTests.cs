using System.Text.Json;
using De.Hochstaetter.HomeAutomationClient;
using De.Hochstaetter.HomeAutomationClient.Misc;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// What the desktop window remembers of its size between two runs, and what it refuses to remember.
/// </summary>
public sealed class StoredWindowSizeTests : IDisposable
{
    private readonly TempFileCache cache = new();

    public void Dispose() => cache.Dispose();

    [Fact]
    public void The_size_of_the_closed_window_is_the_size_of_the_next_one()
    {
        StoredWindowSize.Save(cache, 1280, 800, isMaximized: false);

        var loaded = StoredWindowSize.Load(cache);

        Assert.NotNull(loaded);
        Assert.Equal(1280, loaded.Width);
        Assert.Equal(800, loaded.Height);
        Assert.False(loaded.IsMaximized);
    }

    [Fact]
    public void A_maximized_window_comes_back_maximized_with_the_size_it_had_before()
    {
        StoredWindowSize.Save(cache, 1280, 800, isMaximized: false);
        // Closed while maximized: the client size is the screen's, and must not replace the 1280 x 800.
        StoredWindowSize.Save(cache, 2560, 1400, isMaximized: true);

        var loaded = StoredWindowSize.Load(cache);

        Assert.NotNull(loaded);
        Assert.Equal(1280, loaded.Width);
        Assert.Equal(800, loaded.Height);
        Assert.True(loaded.IsMaximized);
    }

    [Fact]
    public void A_window_maximized_at_its_first_close_keeps_the_only_size_it_has()
    {
        StoredWindowSize.Save(cache, 2560, 1400, isMaximized: true);

        var loaded = StoredWindowSize.Load(cache);

        Assert.NotNull(loaded);
        Assert.Equal(2560, loaded.Width);
        Assert.True(loaded.IsMaximized);
    }

    [Fact]
    public void The_stored_size_is_cut_down_to_the_screen_it_opens_on()
    {
        StoredWindowSize.Save(cache, 3000, 2000, isMaximized: false);

        var loaded = StoredWindowSize.Load(cache, maximumWidth: 1920, maximumHeight: 1040);

        Assert.NotNull(loaded);
        Assert.Equal(1920, loaded.Width);
        Assert.Equal(1040, loaded.Height);
    }

    [Fact]
    public void Nothing_stored_means_nothing_restored()
    {
        Assert.Null(StoredWindowSize.Load(cache));
        Assert.Null(StoredWindowSize.Load(null));
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(double.NaN, 600)]
    [InlineData(double.PositiveInfinity, 600)]
    [InlineData(-800, -600)]
    public void A_size_too_small_to_use_is_neither_stored_nor_restored(double width, double height)
    {
        StoredWindowSize.Save(cache, width, height, isMaximized: false);
        Assert.Null(StoredWindowSize.Load(cache));

        // The same values from a hand edited cache file.
        File.WriteAllText(Path.Combine(cache.Directory, "cache.json"), $$"""{{CacheKeys.WindowSize}}={"Width":{{JsonSerializer.Serialize(width, CacheJson.Options)}},"Height":{{JsonSerializer.Serialize(height, CacheJson.Options)}}}""" + Environment.NewLine);
        Assert.Null(StoredWindowSize.Load(cache));
    }
}
