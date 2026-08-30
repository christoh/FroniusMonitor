using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace De.Hochstaetter.HomeAutomationClient;

/// <summary>
/// An <see cref="ICache"/> that keeps one "key=json" pair per line in a text file. Every head that has a file
/// system uses it; only the directory of the file is platform specific, so that is all a head has to supply.
/// </summary>
/// <remarks>
/// <para>
/// The directory must be the data directory of the app, never the cache directory that the name of
/// <see cref="ICache"/> suggests: Android and iOS delete the latter whenever they need the space, and what we
/// keep here (the connection to the server, for instance) has to survive that.
/// </para>
/// <para>
/// Serialization is reflection based, which needs the types of the cached values to survive the trimmer. All
/// heads that use this class build with a trim mode that leaves our own assemblies alone; give the class a
/// <see cref="JsonSerializerContext"/> before that changes.
/// </para>
/// </remarks>
public abstract class FileCache : ICache
{
    private static readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string cacheFilePath;

    protected FileCache(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        // Avalonia.Controls.Shapes.Path is globally imported, so System.IO.Path needs its full name here.
        cacheFilePath = System.IO.Path.Combine(dataDirectory, "cache.json");

        if (!File.Exists(cacheFilePath))
        {
            using var _ = File.Create(cacheFilePath);
        }
    }

    public void AddOrUpdate(string key, object value)
    {
        var cacheData = LoadCache();
        cacheData[key] = JsonSerializer.Serialize(value, jsonSerializerOptions);
        SaveCache(cacheData);
    }

    public async Task AddOrUpdateAsync(string key, object value, CancellationToken token = default)
    {
        var cacheData = await LoadCacheAsync(token).ConfigureAwait(false);
        cacheData[key] = JsonSerializer.Serialize(value, jsonSerializerOptions);
        await SaveCacheAsync(cacheData, token).ConfigureAwait(false);
    }

    public T? Get<T>(string key)
    {
        var cacheData = LoadCache();
        return cacheData.TryGetValue(key, out var jsonValue) ? JsonSerializer.Deserialize<T>(jsonValue) : default;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken token = default)
    {
        var cacheData = await LoadCacheAsync(token).ConfigureAwait(false);
        return await Task.Run(() => cacheData.TryGetValue(key, out var jsonValue) ? JsonSerializer.Deserialize<T>(jsonValue) : default, token).ConfigureAwait(false);
    }

    private Dictionary<string, string> LoadCache()
    {
        var lines = File.ReadAllLines(cacheFilePath, Encoding.UTF8);
        return ToDictionary(lines);
    }

    private async ValueTask<Dictionary<string, string>> LoadCacheAsync(CancellationToken token)
    {
        var lines = await File.ReadAllLinesAsync(cacheFilePath, Encoding.UTF8, token).ConfigureAwait(false);
        return await Task.Run(() => ToDictionary(lines), token).ConfigureAwait(false);
    }

    private void SaveCache(Dictionary<string, string> cacheData)
    {
        var lines = cacheData.Select(kvp => $"{kvp.Key}={kvp.Value}");
        File.WriteAllLines(cacheFilePath, lines, Encoding.UTF8);
    }

    private async ValueTask SaveCacheAsync(Dictionary<string, string> cacheData, CancellationToken token)
    {
        var lines = await Task.Run(() => cacheData.Select(kvp => $"{kvp.Key}={kvp.Value}"), token).ConfigureAwait(false);
        await File.WriteAllLinesAsync(cacheFilePath, lines, Encoding.UTF8, token).ConfigureAwait(false);
    }

    private static Dictionary<string, string> ToDictionary(string[] lines)
    {
        return new(lines.Select(l =>
        {
            var split = l.Split('=', 2);
            return new KeyValuePair<string, string>(split[0], split.Length > 1 ? split[1] : string.Empty);
        }));
    }
}
