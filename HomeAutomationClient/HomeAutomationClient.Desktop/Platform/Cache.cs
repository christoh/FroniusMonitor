using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace De.Hochstaetter.HomeAutomationClient.Desktop.Platform;

public class Cache : ICache
{
    private static readonly string cacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Hochstätter", "HomeAutomationClient");
    private static readonly string cacheFilePath = Path.Combine(cacheDirectory, "cache.json");

    public Cache()
    {
        Directory.CreateDirectory(cacheDirectory);

        if (!File.Exists(cacheFilePath))
        {
            using var _ = File.Create(cacheFilePath);
        }
    }

    public void AddOrUpdate(string key, object value)
    {
        var cacheData = LoadCache();
        cacheData[key] = JsonConvert.SerializeObject(value);
        SaveCache(cacheData);
    }

    public async Task AddOrUpdateAsync(string key, object value, CancellationToken token = default)
    {
        var cacheData = await LoadCacheAsync(token).ConfigureAwait(false);
        await Task.Run(() => cacheData[key] = JsonConvert.SerializeObject(value), token).ConfigureAwait(false);
        await SaveCacheAsync(cacheData, token).ConfigureAwait(false);
    }

    public T? Get<T>(string key)
    {
        var cacheData = LoadCache();
        return cacheData.TryGetValue(key, out var jsonValue) ? JsonConvert.DeserializeObject<T>(jsonValue) : default;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken token = default)
    {
        var cacheData = await LoadCacheAsync(token).ConfigureAwait(false);
        return await Task.Run(() => cacheData.TryGetValue(key, out var jsonValue) ? JsonConvert.DeserializeObject<T>(jsonValue) : default, token).ConfigureAwait(false);
    }

    private static Dictionary<string, string> LoadCache()
    {
        var lines = File.ReadAllLines(cacheFilePath, Encoding.UTF8);
        return ToDictionary(lines);
    }

    private static async ValueTask<Dictionary<string, string>> LoadCacheAsync(CancellationToken token)
    {
        var lines = await File.ReadAllLinesAsync(cacheFilePath, Encoding.UTF8, token).ConfigureAwait(false);
        return await Task.Run(() => ToDictionary(lines), token).ConfigureAwait(false);
    }

    private static void SaveCache(Dictionary<string, string> cacheData)
    {
        var lines = cacheData.Select(kvp =>
        {
            return $"{kvp.Key}={kvp.Value}";
        });
        
        File.WriteAllLines(cacheFilePath, lines, Encoding.UTF8);
    }

    private static async ValueTask SaveCacheAsync(Dictionary<string, string> cacheData, CancellationToken token)
    {
        var lines = await Task.Run(() => cacheData.Select(kvp => $"{kvp.Key}={kvp.Value}"), token);
        await File.WriteAllLinesAsync(cacheFilePath, lines, Encoding.UTF8, token).ConfigureAwait(false);
    }

    private static Dictionary<string, string> ToDictionary(string[] lines)
    {
        return new(lines.Select(l =>
        {
            var split = l.Split('=', 2, StringSplitOptions.None);
            return new KeyValuePair<string, string>(split[0], split.Length > 1 ? split[1] : string.Empty);
        }));
    }
}
