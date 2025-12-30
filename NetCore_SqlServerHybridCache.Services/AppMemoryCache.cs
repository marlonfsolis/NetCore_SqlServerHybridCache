using Microsoft.Extensions.Caching.Memory;

namespace NetCore_SqlServerHybridCache.Services.Extensions;

public class AppMemoryCache : IAppMemoryCache
{
    private readonly IMemoryCache _memoryCache;

    public AppMemoryCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
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

    public IEnumerable<string> GetKeys();
    {
        
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