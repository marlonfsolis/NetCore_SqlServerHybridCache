namespace NetCore_SqlServerHybridCache.Services;

public interface IAppMemoryCache
{
    T? Get<T>(string key);
    
    (bool Success, T? Value) TryGet<T>(string key);
    
    IEnumerable<string> GetKeys();
    
    IEnumerable<string> GetKeys(MemoryCacheKeySearchType searchType, string partialKey);
    
    void Set<T>(string key, T value);
    
    void Set<T>(string key, T value, TimeSpan expiration);
    
    void Remove(string key);
}