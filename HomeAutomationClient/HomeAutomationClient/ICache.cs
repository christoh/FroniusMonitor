namespace De.Hochstaetter.HomeAutomationClient;

public interface ICache
{
    void AddOrUpdate(string key, object value);
    Task AddOrUpdateAsync(string key, object value, CancellationToken token = default);
    T? Get<T>(string key);
    Task<T?> GetAsync<T>(string key, CancellationToken token = default);

    /// <summary>Forgets <paramref name="key"/> and its value. A key that is not there is not an error.</summary>
    Task RemoveAsync(string key, CancellationToken token = default);
}
