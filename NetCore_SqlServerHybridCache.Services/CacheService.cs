using NetCore_SqlServerHybridCache.Services.Models;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;

namespace NetCore_SqlServerHybridCache.Services;

public class CacheService : ICacheService
{
    private readonly IAppMemoryCache _localCache;
    private readonly IConfiguration _configuration;
    private readonly string _prefix;
    private readonly string _connectionString;
    private TimeSpan _absoluteExpirationRelativeToNow;

    // We need to lock per key. Only for write operations.
    // This will prevent multiple threads trying to update the same key at the same time.
    // And prevent error on DB transaction when using Memory Optimized table.
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks;

    private long _lastTrackingNo;
    private DateTime _lastRefreshTime;

    public CacheService(
        IAppMemoryCache localCache,
        IConfiguration configuration,
        string? prefix,
        TimeSpan absoluteExpirationRelativeToNow)
    {
        _localCache = localCache;
        _configuration = configuration;
        _prefix = prefix ?? string.Empty;
        _absoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
        _connectionString = GetConnectionString();
        _locks = new ConcurrentDictionary<string, SemaphoreSlim>();
    }


    /* Property section *********************************************************/

    public string Prefix => _prefix;

    public TimeSpan AbsoluteExpirationRelativeToNow
    {
        get => _absoluteExpirationRelativeToNow;
        set => _absoluteExpirationRelativeToNow = value;
    }


    /* Private Method section *********************************************************/

    private string GetConnectionString()
    {
        string? connectionString = _configuration.GetConnectionString("HybridCache");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString));
        }

        return connectionString;
    }

    private SqlConnection GetConnection()
    {
        return new SqlConnection(_connectionString);
    }

    private string GetKey(string key)
    {
        return $"{Prefix}{key}";
    }

    private async Task LogError(string? message, string? detail)
    {
        DynamicParameters dynParams = new DynamicParameters();
        dynParams.Add("@ErrorMessage", message);
        dynParams.Add("@ErrorDetail", detail);

        const string sql = "dbo.usp_logError";
        SqlConnection connection = GetConnection();
        await connection.ExecuteAsync(sql, dynParams, commandType: CommandType.StoredProcedure);
    }

    private async Task<bool> CacheKeyExistsInSource(string key)
    {
        try
        {
            DynamicParameters dynParams = new DynamicParameters();
            dynParams.Add("@Key", key);

            const string sql = "dbo.usp_getCacheKeyExists";
            using IDbConnection connection = GetConnection();
            bool exists =
                await connection.QueryFirstAsync<bool>(sql, dynParams, commandType: CommandType.StoredProcedure);

            return exists;
        }
        catch (Exception e)
        {
            await LogError(e.Message, e.StackTrace);
            return false;
        }
    }

    private async Task<IEnumerable<string>> GetCacheKeyListFromSource()
    {
        try
        {
            DynamicParameters dynParams = new DynamicParameters();

            const string sql = "dbo.usp_getCacheKeyList";
            using IDbConnection connection = GetConnection();
            IEnumerable<string> list =
                await connection.QueryAsync<string>(sql, dynParams, commandType: CommandType.StoredProcedure);

            return list;
        }
        catch (Exception e)
        {
            await LogError(e.Message, e.StackTrace);
            return [];
        }
    }

    private async Task<IEnumerable<CacheChange>> GetCacheChangesFromSource()
    {
        try
        {
            DynamicParameters dynParams = new DynamicParameters();
            dynParams.Add("@LastTrackingNo", _lastTrackingNo);

            const string sql = "dbo.usp_getCacheChanges";
            using IDbConnection connection = GetConnection();
            IEnumerable<CacheChange> changes = await connection.QueryAsync<CacheChange>(
                sql, dynParams, commandType: CommandType.StoredProcedure);

            return changes;
        }
        catch (Exception e)
        {
            await LogError(e.Message, e.StackTrace);
            return [];
        }
    }

    private async Task<T?> GetFromSourceAsync<T>(string key, CancellationToken token = default)
    {
        try
        {
            if (token.IsCancellationRequested)
            {
                return default(T);
            }

            DynamicParameters dynParams = new DynamicParameters();
            dynParams.Add("@Key", key);
            dynParams.Add("@UtcNow", DateTime.UtcNow);

            const string sql = "dbo.usp_getCacheValue";
            using IDbConnection connection = GetConnection();
            byte[]? bytes =
                await connection.QueryFirstOrDefaultAsync<byte[]>(sql, dynParams,
                    commandType: CommandType.StoredProcedure);
            if (bytes is null || bytes.Length == 0)
            {
                return default(T);
            }

            using MemoryStream ms = new(bytes);
            T? result = JsonSerializer.Deserialize<T>(ms);

            return result;
        }
        catch (Exception e)
        {
            await LogError(e.Message, e.StackTrace);
            return default;
        }
    }

    private async Task UpdateSourceByKey<T>(string key, T value, TimeSpan expirationTime)
    {
        try
        {
            using MemoryStream ms = new();
            await JsonSerializer.SerializeAsync(ms, value);

            string dataType = $"{typeof(T).FullName}, {typeof(T).Assembly.GetName().Name}";

            DynamicParameters dynParams = new DynamicParameters();
            dynParams.Add("@Key", key);
            dynParams.Add("@Value", ms.ToArray());
            dynParams.Add("@AbsoluteExpiration", DateTime.UtcNow.Add(expirationTime));
            dynParams.Add("@DataType", dataType);

            const string sql = "dbo.usp_setCacheValue";
            IDbConnection connection = GetConnection();
            await connection.ExecuteAsync(sql, dynParams, commandType: CommandType.StoredProcedure);
        }
        catch (Exception e)
        {
            await LogError(e.Message, e.StackTrace);
        }
    }

    private async Task RemoveFromSource(string key)
    {
        try
        {
            DynamicParameters dynParams = new DynamicParameters();
            dynParams.Add("@Key", key);

            const string sql = "usp_deleteCacheValue";
            IDbConnection connection = GetConnection();
            await connection.ExecuteAsync(sql, dynParams, commandType: CommandType.StoredProcedure);
        }
        catch (Exception e)
        {
            await LogError(e.Message, e.StackTrace);
        }
    }

    private async Task ClearFromSource()
    {
        try
        {
            DynamicParameters dynParams = new DynamicParameters();

            const string sql = "usp_clearCache";
            IDbConnection connection = GetConnection();
            await connection.ExecuteAsync(sql, dynParams, commandType: CommandType.StoredProcedure);
        }
        catch (Exception e)
        {
            await LogError(e.Message, e.StackTrace);
        }
    }


    /* Public Method section *********************************************************/

    public async Task RefreshLocalCacheFromSource()
    {
        try
        {
            DateTime utcNow = DateTime.UtcNow;

            double elapsedSeconds = (utcNow - _lastRefreshTime).TotalSeconds;
            if (elapsedSeconds < 5)
            {
                return;
            }

            Debug.WriteLine("Checking changes...");

            // Get current version from remote cache
            IEnumerable<CacheChange> remoteChanges = await GetCacheChangesFromSource();
            if (remoteChanges.Any())
            {
                // Remote cache has changes. Update local cache for this session.
                Parallel.ForEach(remoteChanges, (change) =>
                {
                    if (change.CacheValue is null || change.CacheValue.Length == 0)
                    {
                        Remove(change.AppCacheKey);
                        return;
                    }

                    if (change.DataType.IsNullOrEmptyOrWhiteSpace())
                    {
                        return;
                    }

                    Type? type = Type.GetType(change.DataType);
                    if (type == null)
                    {
                        type = AppDomain.CurrentDomain.GetAssemblies()
                            .Select(a => a.GetType(change.DataType, false, true))
                            .FirstOrDefault(t => t is not null);
                    }

                    if (type is null)
                    {
                        return;
                    }

                    using MemoryStream ms = new(change.CacheValue);
                    ms.Position = 0;
                    object? result = JsonSerializer.Deserialize(ms, type);
                    if (result is null)
                    {
                        return;
                    }

                    _localCache.Set(change.AppCacheKey, result);
                });

                // Update last checked version
                long remoteTrackingNo = remoteChanges.First().TrackingNo;
                _lastTrackingNo = remoteTrackingNo;

                Debug.WriteLine($"Got changes with TrackingNo: {remoteTrackingNo}.");
            }

            // Update last checked time
            _lastRefreshTime = utcNow;
        }
        catch (Exception e)
        {
            await LogError(e.Message, e.StackTrace);
        }
    }

    public T? Get<T>(string key)
    {
        return GetAsync<T>(key).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        (bool result, T? value) response = await TryGetAsync<T>(key);
        return response.value;
    }

    public (bool result, T? value) TryGet<T>(string key)
    {
        return TryGetAsync<T>(key).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task<(bool result, T? value)> TryGetAsync<T>(string key)
    {
        try
        {
            string _key = GetKey(key);

            // Try local cache first
            (bool Success, T? Value) _localRes = _localCache.TryGet<T>(_key);
            if (_localRes.Success && _localRes.Value != null)
            {
                return _localRes;
            }

            // Try remote cache
            T? remoteVal = await GetFromSourceAsync<T>(_key);
            if (remoteVal == null) return (false, default(T));

            // Save it in local cache for next use
            _localCache.Set(_key, remoteVal);
            return (true, remoteVal);
        }
        catch (Exception e)
        {
            await LogError(e.Message, e.StackTrace);
            return (false, default(T));
        }
    }

    public IEnumerable<string> GetKeys(bool removePrefix = false)
    {
        return GetKeysAsync(removePrefix).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task<IEnumerable<string>> GetKeysAsync(bool removePrefix = false)
    {
        try
        {
            IEnumerable<string> keys = await GetCacheKeyListFromSource();
            if (removePrefix)
            {
                keys = keys.Select(k => k.StartsWith(Prefix) ? k.Substring(Prefix.Length) : k);
            }

            return keys;
        }
        catch (Exception e)
        {
            await LogError(e.Message, e.StackTrace);
            return [];
        }
    }

    public async Task<bool> KeyExistsAsync(string key)
    {
        try
        {
            string _key = GetKey(key);
            bool keyExists = await CacheKeyExistsInSource(_key);
            return keyExists;
        }
        catch (Exception e)
        {
            await LogError(e.Message, e.StackTrace);
            return false;
        }
    }

    public void Set<T>(string key, T value)
    {
        Set(key, value, AbsoluteExpirationRelativeToNow);
    }

    public async Task SetAsync<T>(string key, T value)
    {
        await SetAsync(key, value, AbsoluteExpirationRelativeToNow);
    }

    public void Set<T>(string key, T value, TimeSpan absoluteExpirationRelativeToNow)
    {
        SetAsync(key, value, absoluteExpirationRelativeToNow).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan absoluteExpirationRelativeToNow)
    {
        try
        {
            if (key.IsNullOrEmptyOrWhiteSpace()) return;

            if (value is null)
            {
                await RemoveAsync(key);
                return;
            }

            // Set in local cache
            string _key = GetKey(key);
            _localCache.Set(_key, value, absoluteExpirationRelativeToNow);


            // Set in remote session cache.
            SemaphoreSlim semaphore = _locks.GetOrAdd(_key, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();
            try
            {
                await UpdateSourceByKey(_key, value, absoluteExpirationRelativeToNow);
            }
            finally
            {
                semaphore.Release();
            }
        }
        catch (Exception e)
        {
            await LogError(e.Message, e.StackTrace);
        }
    }

    public void Remove(string key)
    {
        RemoveAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            string _key = GetKey(key);

            SemaphoreSlim semaphore = _locks.GetOrAdd(_key, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();
            try
            {
                await RemoveFromSource(_key);
            }
            finally
            {
                semaphore.Release();
            }
        }
        catch (Exception e)
        {
            await LogError(e.Message, e.StackTrace);
        }
    }

    public void RemoveLocal(string key)
    {
        string _key = GetKey(key);
        _localCache.Remove(_key);
    }

    public void Clear()
    {
        ClearAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task ClearAsync()
    {
        try
        {
            // Remove all local cache keys for this session.
            IEnumerable<string> keys = await GetKeysAsync();
            Parallel.ForEach(keys, (key) =>
            {
                _localCache.Remove(key);
            });

            // Lock all keys
            foreach (KeyValuePair<string, SemaphoreSlim> l in _locks)
            {
                await l.Value.WaitAsync();
            }

            // Clear remote session cache.
            try
            {
                await ClearFromSource();
            }
            finally
            {
                // Release all keys
                _locks.Clear();
            }
        }
        catch (Exception e)
        {
            await LogError(e.Message, e.StackTrace);
        }
    }
}