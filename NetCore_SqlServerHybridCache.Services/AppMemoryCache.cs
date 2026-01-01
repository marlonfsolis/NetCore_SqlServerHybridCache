using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace NetCore_SqlServerHybridCache.Services.Extensions;

public class AppMemoryCache : IAppMemoryCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly string _keys_keyName = "ApplicationKeys_12DF24MFVG334SDF34";
    private readonly ConcurrentDictionary<string, byte> _keys;

    public AppMemoryCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;

        if (!_memoryCache.TryGetValue(_keys_keyName, out ConcurrentDictionary<string, byte>? keys) || keys == null)
        {
            _keys = new ConcurrentDictionary<string, byte>();
            _memoryCache.Set(_keys_keyName, _keys);
        }
        else
        {
            _keys = keys;
        }
    }
    
    public T? Get<T>(string key)
    {
        (bool Success, T? Value) result = TryGet<T>(key);
        return result.Value;
    }

    public (bool Success, T? Value) TryGet<T>(string key)
    {
        bool success =_memoryCache.TryGetValue<T>(key, out T? value);
        return (success, value);
    }

    public IEnumerable<string> GetKeys()
    {
        return _keys.Keys;
    }

    public IEnumerable<string> GetKeys(MemoryCacheKeySearchType searchType, string partialKey)
    {
        IEnumerable<string> result;
        if (!_memoryCache.TryGetValue<ConcurrentDictionary<string, byte>>(_keys_keyName, out var keyDict) || keyDict == null)
        {
            return Enumerable.Empty<string>();
        }

        switch (searchType)
        {
            case MemoryCacheKeySearchType.StartsWith:
                result = keyDict.Keys.Where(x => x.StartsWith(partialKey));
                break;
            case MemoryCacheKeySearchType.EndsWith:
                result = keyDict.Keys.Where(x => x.EndsWith(partialKey));
                break;
            case MemoryCacheKeySearchType.Contains:
                result = keyDict.Keys.Where(x => x.Contains(partialKey));
                break;
            default:
                result = keyDict.Keys;
                break;
        }

        return result;
    }

public void Set<T>(string key, T value)
    {
        _memoryCache.Set(key, value);
    }

    public void Set<T>(string key, T value, TimeSpan expiration)
    {
        _memoryCache.Set(key, value, expiration);
    }

    public void Remove(string key)
    {
        _memoryCache.Remove(key);
    }
}