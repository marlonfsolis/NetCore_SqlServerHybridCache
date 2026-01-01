namespace NetCore_SqlServerHybridCache.Services;

public interface ICacheService
{
    /// <summary>
    /// Get value from cache by given key.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    T? Get<T>(string key);

    /// <summary>
    /// Get value from cache by given key asynchronously.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Try to get the value from cache by given key. Return if was found or not.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    (bool result, T? value) TryGet<T>(string key);

    /// Try to get the value from cache by given key. Return if was found or not asynchronously.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    Task<(bool result, T? value)> TryGetAsync<T>(string key);

    /// <summary>
    /// Get a list of existing keys in cache for one session.
    /// </summary>
    /// <returns></returns>
    IEnumerable<string> GetKeys();

    /// <summary>
    /// Get a list of existing keys in cache for one session asynchronously.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<string>> GetKeysAsync();

    /// <summary>
    /// Asynchronously determines whether the specified key exists in the data store.
    /// </summary>
    /// <param name="key">The key to locate in the data store. Cannot be null or empty.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains <see langword="true"/> if the key
    /// exists; otherwise, <see langword="false"/>.</returns>
    Task<bool> KeyExistsAsync(string key);

    /// <summary>
    /// Create or overwrite an entry in the cache.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    void Set(string key, object value);

    /// <summary>
    /// Create or overwrite an entry in the cache asynchronously.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    Task SetAsync(string key, object value);

    /// <summary>
    /// Create or overwrite an entry in the cache.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="absoluteExpirationRelativeToNow"></param>
    void Set(string key, object value, TimeSpan absoluteExpirationRelativeToNow);

    /// <summary>
    /// Create or overwrite an entry in the cache asynchronously.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="absoluteExpirationRelativeToNow"></param>
    Task SetAsync(string key, object value, TimeSpan absoluteExpirationRelativeToNow);


    /// <summary>
    /// Remove an entry in cache by key.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    Task Remove(string key);

    /// <summary>
    /// Remove an entry in cache by key asynchronously.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    Task RemoveAsync(string key);

    /// <summary>
    /// Remove the Key from Local Cache
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    void RemoveLocal(string key);

    /// <summary>
    /// Remove all entries in cache.
    /// </summary>
    void Clear();

    /// <summary>
    /// Remove all entries in cache asynchronously.
    /// </summary>
    Task ClearAsync();
}
