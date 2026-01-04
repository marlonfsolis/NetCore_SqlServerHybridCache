namespace NetCore_SqlServerHybridCache.Services;

public interface ISessionService
{
    /// <summary>
    /// Creates session record.
    /// </summary>
    void CreateSession();

    /// <summary>
    /// Creates session record asynchronously.
    /// </summary>
    /// <returns></returns>
    Task CreateSessionAsync();

    /// <summary>
    /// Creates session record asynchronously.
    /// </summary>
    /// param name="id" Session ID
    /// <returns></returns>
    Task CreateSessionAsync(string id);

    /// <summary>
    /// Get value from cache by given key.
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    TItem? Get<TItem>(string key);

    /// <summary>
    /// Get value from cache by given key asynchronously.
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    Task<TItem?> GetAsync<TItem>(string key);

    /// <summary>
    /// Try to get the value from cache by given key. Return if was found or not.
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    (bool result, TItem? value) TryGet<TItem>(string key);

    /// Try to get the value from cache by given key. Return if was found or not asynchronously.
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    Task<(bool result, TItem? value)> TryGetAsync<TItem>(string key);

    /// Try to get the value from cache by given sessionId and key. Return if was found or not asynchronously.
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    Task<(bool result, TItem? value)> TryGetAsync<TItem>(string id, string key);

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
    /// Get a list of existing keys in cache for one session asynchronously.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<string>> GetKeysAsync(string id);

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
    /// Create or overwrite an entry in the cache asynchronously.
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="absoluteExpirationRelativeToNow"></param>
    Task SetAsync(string id, string key, object value, TimeSpan absoluteExpirationRelativeToNow);

    /// <summary>
    /// RefreshAsync the session with a new expiration time.
    /// </summary>
    void RefreshSession();

    /// <summary>
    /// RefreshAsync the session with a new expiration time asynchronously.
    /// </summary>
    Task RefreshSessionAsync();

    /// <summary>
    /// RefreshAsync the session with a new expiration time asynchronously.
    /// </summary>
    Task RefreshSessionAsync(string id);

    /// <summary>
    /// Remove an entry in cache by key.
    /// </summary>
    /// <param name="key"></param>
    void Remove(string key);

    /// <summary>
    /// Remove an entry in cache by key asynchronously.
    /// </summary>
    /// <param name="key"></param>
    Task RemoveAsync(string key);

    /// <summary>
    /// Remove an entry in cache by key asynchronously.
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <param name="key"></param>
    Task RemoveAsync(string id, string key);

    /// <summary>
    /// Remove all entries in cache.
    /// </summary>
    void Clear();

    /// <summary>
    /// Remove all entries in cache asynchronously.
    /// </summary>
    Task ClearAsync();

    /// <summary>
    /// Remove all entries in cache asynchronously.
    /// </summary>
    Task ClearAsync(string id);

    /// <summary>
    /// Delete all entries in cache and the session record.
    /// </summary>
    /// <returns></returns>
    void Delete();

    /// <summary>
    /// Delete all entries in cache and the session record asynchronously.
    /// </summary>
    /// <returns></returns>
    Task DeleteAsync();

    /// <summary>
    /// Delete all entries in cache and the session record asynchronously.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task DeleteAsync(string id);

    /// <summary>
    /// Set a new default sliding expiration time for the session cache.
    /// This is helpful when you want to have a different sliding expiration time than the one set in configuration.
    /// </summary>
    /// <param name="slidingExpiration"></param>
    void SetDefaultSlidingExpiration(TimeSpan slidingExpiration);
}
