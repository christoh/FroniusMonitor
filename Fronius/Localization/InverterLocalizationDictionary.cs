using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Localization;

public class InverterLocalizationDictionary : IReadOnlyDictionary<string, string>
{
    public InverterLocalizationDictionary(JObject dictionary)
    {
        Dictionary = dictionary;
    }

    public JObject Dictionary { get; }


    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        return Dictionary.OfType<KeyValuePair<string, JToken?>>().Select(k => new KeyValuePair<string, string>(k.Key, k.Value?.Value<string>() ?? string.Empty)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => Dictionary.Count;

    public bool ContainsKey(string key) => Dictionary.ContainsKey(key);

    public bool TryGetValue(string key, out string value)
    {
        if (!Dictionary.TryGetValue(key, out var token))
        {
            value = key;
            return false;
        }

        value = token?.Value<string>() ?? string.Empty;
        return true;
    }

    public string this[string key] => Dictionary[key]?.Value<string>() ?? key;

    public IEnumerable<string> Keys => Dictionary.OfType<KeyValuePair<string, JToken>>().Select(k => k.Key);
    public IEnumerable<string> Values => Dictionary.OfType<KeyValuePair<string, JToken>>().Select(k => k.Value.Value<string>() ?? k.Key);
}
