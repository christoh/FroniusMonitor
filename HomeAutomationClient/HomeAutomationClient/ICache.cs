using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.HomeAutomationClient;

public interface ICache
{
    void AddOrUpdate(string key, object value);
    Task AddOrUpdateAsync(string key, object value, CancellationToken token = default);
    T? Get<T>(string key);
    Task<T?> GetAsync<T>(string key, CancellationToken token = default);
}