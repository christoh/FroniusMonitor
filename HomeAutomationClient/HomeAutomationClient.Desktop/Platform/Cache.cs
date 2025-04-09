using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace De.Hochstaetter.HomeAutomationClient.Desktop.Platform;

public class Cache : ICache
{
    private static readonly string cacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Hochstaetter", "HomeAutomationClient");
    private static readonly string cacheFilePath = Path.Combine(cacheDirectory, "cache.json");

    public Cache()
    {
        Directory.CreateDirectory(cacheDirectory);
        
        if (!File.Exists(cacheFilePath))
        {
            File.WriteAllText(cacheFilePath, "{}");
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
        var json = File.ReadAllText(cacheFilePath);
        return JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
    }

    private static async ValueTask<Dictionary<string, string>> LoadCacheAsync(CancellationToken token)
    {
        var json = await File.ReadAllTextAsync(cacheFilePath, token).ConfigureAwait(false);
        return await Task.Run(() => JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>(), token).ConfigureAwait(false);
    }

    private static void SaveCache(Dictionary<string, string> cacheData)
    {
        var json = JsonConvert.SerializeObject(cacheData, Formatting.Indented);
        File.WriteAllText(cacheFilePath, json);
    }

    private static async ValueTask SaveCacheAsync(Dictionary<string, string> cacheData, CancellationToken token)
    {
        var json = await Task.Run(() => JsonConvert.SerializeObject(cacheData, Formatting.Indented), token).ConfigureAwait(false);
        await File.WriteAllTextAsync(cacheFilePath, json, token).ConfigureAwait(false);
    }
}
