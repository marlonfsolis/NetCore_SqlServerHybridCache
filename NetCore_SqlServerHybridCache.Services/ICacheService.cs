namespace NetCore_SqlServerHybridCache.Services;

public interface ICacheService
{
    /// <summary>
    /// Get Data using key
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    Task<T?> Get<T>(string key);

    /// <summary>
    /// Set Data with Value with no Expiration Time
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    Task Set(string key, object value);

    /// <summary>
    /// Set Data with Value and Expiration Time of Key
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="expirationTime"></param>
    /// <returns></returns>
    Task Set(string key, object value, TimeSpan expirationTime);

}
