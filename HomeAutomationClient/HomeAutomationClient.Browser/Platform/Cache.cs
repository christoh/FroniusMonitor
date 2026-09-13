using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace De.Hochstaetter.HomeAutomationClient.Browser.Platform;

public partial class Cache: ICache
{
    [JSImport("globalThis.localStorage.setItem")]
    private static partial void SetItem(string key, string value);

    [JSImport("globalThis.localStorage.getItem")]
    private static partial string GetItem(string key);

    public void AddOrUpdate(string key, object value)
    {
        SetItem(key, JsonSerializer.Serialize(value, CacheJson.Options));
    }

    public Task AddOrUpdateAsync(string key, object value, CancellationToken token = default)
    {
        AddOrUpdate(key, value);
        return Task.CompletedTask;
    }
    
    public Task<T?> GetAsync<T>(string key, CancellationToken token = default)
    {
        return Task.FromResult(Get<T?>(key));
    }

    public T? Get<T>(string key)
    {
        var jsonValue = GetItem(key);
        return string.IsNullOrEmpty(jsonValue) ? default : JsonSerializer.Deserialize<T>(jsonValue, CacheJson.Options);
    }
}