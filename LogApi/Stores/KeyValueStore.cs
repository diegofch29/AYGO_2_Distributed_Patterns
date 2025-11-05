using System.Collections.Concurrent;

namespace LogApi.Stores;

public class KeyValueStore
{
    private readonly ConcurrentDictionary<string, string> _data = new();

    public bool AddOrUpdate(string key, string value)
    {
        _data[key] = value;
        return true;
    }

    public string? Get(string key) => _data.TryGetValue(key, out var value) ? value : null;

    public IReadOnlyDictionary<string, string> GetAll() => _data;
}
