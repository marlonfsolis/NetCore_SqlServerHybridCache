using Microsoft.AspNetCore.Http;
using NetCore_SqlServerHybridCache.Services.Constants;
using NetCore_SqlServerHybridCache.Services.Models;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;

namespace NetCore_SqlServerHybridCache.Services;

public class SessionService : ISessionService
{
    private readonly IAppMemoryCache _localCache;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _connectionString;
    private TimeSpan _absoluteExpirationRelativeToNow;


    private ConcurrentDictionary<string, long> _lastTrackingNoDic = new();
    private ConcurrentDictionary<string, DateTimeOffset> _lastRefreshTimeDic = new();

    // We need to lock per key in each session id. Only for write operations.
    // This will prevent multiple threads trying to update the same key at the same time.
    // And prevent error on DB transaction when using Memory Optimized table.
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public SessionService(
        IAppMemoryCache localCache,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _localCache = localCache;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _connectionString = GetConnectionString();

        int secondsToExpire = _configuration.GetValue<int>("SessionTimeToExpireInSeconds", 1800);
        _absoluteExpirationRelativeToNow = TimeSpan.FromMinutes(secondsToExpire);
    }


    /* Property section *********************************************************/

    public HttpContext? HttpContext => _httpContextAccessor.HttpContext;

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

    private string GetId()
    {
        if (HttpContext == null)
        {
            return string.Empty;
        }

        string? sessionId = (string?)HttpContext.Items[CacheLiterals.ApplicationSessionIdName];
        if (string.IsNullOrEmpty(sessionId))
        {
            return string.Empty;
        }

        return sessionId;
    }

    private string LocalKey(string id, string key)
    {
        return $"{id}-{key}";
    }

    private async Task<bool> SessionKeyExistsInSource(string id, string key)
    {
        DynamicParameters dynParams = new DynamicParameters();
        dynParams.Add("@Id", id);
        dynParams.Add("@Key", key);

        const string sql = "dbo.usp_getSessionKeyExists";
        using IDbConnection connection = GetConnection();
        bool exists = await connection.QueryFirstAsync<bool>(sql, dynParams, commandType: CommandType.StoredProcedure);

        return exists;
    }

    private async Task<IEnumerable<string>> GetSessionKeyListFromSource(string id)
    {
        DynamicParameters dynParams = new DynamicParameters();
        dynParams.Add("@Id", id);

        const string sql = "dbo.usp_getSessionKeyList";
        using IDbConnection connection = GetConnection();
        IEnumerable<string> list = await connection.QueryAsync<string>(sql, dynParams, commandType: CommandType.StoredProcedure);

        return list;
    }

    private async Task<IEnumerable<SessionChange>> GetSessionChangesFromSource(string id, long lastTrackingNo)
    {
        DynamicParameters dynParams = new DynamicParameters();
        dynParams.Add("@Id", id);
        dynParams.Add("@LastTrackingNo", lastTrackingNo);

        const string sql = "dbo.usp_getSessionChanges";
        using IDbConnection connection = GetConnection();
        IEnumerable<SessionChange> changes = await connection.QueryAsync<SessionChange>(sql, dynParams, commandType: CommandType.StoredProcedure);

        return changes;
    }

    private async Task<T?> GetFromSourceAsync<T>(string id, string key, CancellationToken token = default)
    {
        if (token.IsCancellationRequested)
        {
            return default(T);
        }

        DynamicParameters dynParams = new DynamicParameters();
        dynParams.Add("@Id", id);
        dynParams.Add("@Key", key);
        dynParams.Add("@UtcNow", DateTimeOffset.UtcNow);

        string sql = "dbo.usp_getSessionValue";
        using IDbConnection connection = GetConnection();
        byte[]? bytes = await connection.QueryFirstOrDefaultAsync<byte[]>(sql, dynParams, commandType: CommandType.StoredProcedure);
        if (bytes is null || bytes.Length == 0)
        {
            return default(T);
        }

        using MemoryStream ms = new(bytes);
        T? result = JsonSerializer.Deserialize<T>(ms);

        return result;
    }

    private async Task UpdateSource<T>(string id, string key, T value, TimeSpan expirationTime)
    {
        using MemoryStream ms = new();
        await JsonSerializer.SerializeAsync(ms, value);

        string dataType = $"{typeof(T).FullName}, {typeof(T).Assembly.GetName().Name}";

        DynamicParameters dynParams = new DynamicParameters();
        dynParams.Add("@Id", id);
        dynParams.Add("@Key", key);
        dynParams.Add("@Value", ms.ToArray());
        dynParams.Add("@AbsoluteExpiration", DateTimeOffset.UtcNow.Add(expirationTime));
        dynParams.Add("@DataType", dataType);

        const string sql = "dbo.usp_setSessionValue";
        IDbConnection connection = GetConnection();
        await connection.ExecuteAsync(sql, dynParams, commandType: CommandType.StoredProcedure);
    }

    private async Task RemoveFromSource(string id, string key)
    {
        DynamicParameters dynParams = new DynamicParameters();
        dynParams.Add("@Id", id);
        dynParams.Add("@Key", key);

        string sql = "dbo.usp_deleteSessionValue";
        IDbConnection connection = GetConnection();
        await connection.ExecuteAsync(sql, dynParams, commandType: CommandType.StoredProcedure);
    }

    private async Task ClearFromSource(string id)
    {
        DynamicParameters dynParams = new DynamicParameters();
        dynParams.Add("@Id", id);

        string sql = "dbo.usp_clearSession";
        IDbConnection connection = GetConnection();
        await connection.ExecuteAsync(sql, dynParams, commandType: CommandType.StoredProcedure);
    }

    private async Task DeleteSessionFromSource(string id)
    {
        DynamicParameters dynParams = new DynamicParameters();
        dynParams.Add("@Id", id);

        string sql = "dbo.usp_deleteSessionCache";
        IDbConnection connection = GetConnection();
        await connection.ExecuteAsync(sql, dynParams, commandType: CommandType.StoredProcedure);
    }

    private async Task RefreshLocalSessionFromSource(string id, long lastTrackingNo, DateTimeOffset utcNow)
    {
        try
        {
            Debug.WriteLine("Checking changes...");

            // Get current version from remote cache
            IEnumerable<SessionChange> remoteChanges = await GetSessionChangesFromSource(id, lastTrackingNo);
            if (remoteChanges.Any())
            {
                // Remote session has changes. Update local cache for this session.
                Parallel.ForEach(remoteChanges, (change, cancel) =>
                {
                    string _key = LocalKey(change.SessionId, change.SessionKey);

                    if (change.SessionValue is null)
                    {
                        _localCache.Remove(_key);
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

                    using MemoryStream ms = new(change.SessionValue);
                    ms.Position = 0;
                    object? result = JsonSerializer.Deserialize(ms, type);
                    if (result is null)
                    {
                        return;
                    }

                    _localCache.Set(_key, result);
                });

                // Update last checked version
                long remoteTrackingNo = remoteChanges.First().TrackingNo;
                _lastTrackingNoDic[id] = remoteTrackingNo;

                Debug.WriteLine($"Got changes with TrackingNo: {remoteTrackingNo}.");
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }
    }

    private async Task RefreshSessionInSource(string id, DateTimeOffset utcNow, TimeSpan expirationTime)
    {
        try
        {
            DynamicParameters dynParams = new DynamicParameters();
            dynParams.Add("@Id", id);
            dynParams.Add("@AbsoluteExpiration", utcNow.Add(expirationTime));
            dynParams.Add("@UtcNow", utcNow);

            string sql = "dbo.usp_refreshSession";
            IDbConnection connection = GetConnection();
            await connection.ExecuteAsync(sql, dynParams, commandType: CommandType.StoredProcedure);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }
    }


    /* Public Method section *********************************************************/


    public T? Get<T>(string key)
    {
        return GetAsync<T>(key).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var response = await TryGetAsync<T>(key);
        return response.value;
    }

    public (bool result, T? value) TryGet<T>(string key)
    {
        return TryGetAsync<T>(key).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task<(bool result, T? value)> TryGetAsync<T>(string key)
    {
        return await TryGetAsync<T>(GetId(), key);
    }

    public async Task<(bool result, T? value)> TryGetAsync<T>(string id, string key)
    {
        string _key = LocalKey(id, key);

        // Try local cache first
        (bool Success, T? Value) _localRes = _localCache.TryGet<T>(_key);
        if (_localRes.Success && _localRes.Value != null)
        {
            return _localRes;
        }

        // Try remote cache
        try
        {
            T? remoteVal = await GetFromSourceAsync<T>(id, _key, CancellationToken.None);
            if (remoteVal != null)
            {
                // Save it in local cache for next use
                _localCache.Set(_key, remoteVal);
                return (true, remoteVal);
            }

            return (false, default(T));
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            return (false, default(T));
        }
    }

    public IEnumerable<string> GetKeys()
    {
        return GetKeysAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task<IEnumerable<string>> GetKeysAsync()
    {
        string id = GetId();
        return await GetKeysAsync(id);
    }

    public async Task<IEnumerable<string>> GetKeysAsync(string id)
    {
        try
        {
            return await GetSessionKeyListFromSource(id);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            return Enumerable.Empty<string>();
        }
    }

    public async Task<bool> KeyExistsAsync(string key)
    {
        try
        {
            string id = GetId();
            string _key = LocalKey(id, key);
            bool keyExists = await SessionKeyExistsInSource(id, _key);

            return keyExists;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            return false;
        }
    }

    public void Set<T>(string key, T value)
    {
        Set(key, value, AbsoluteExpirationRelativeToNow);
    }

    public void Set<T>(string key, T value, TimeSpan absoluteExpirationRelativeToNow)
    {
        SetAsync(key, value, absoluteExpirationRelativeToNow).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task SetAsync<T>(string key, T value)
    {
        await SetAsync(key, value, AbsoluteExpirationRelativeToNow);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan absoluteExpirationRelativeToNow)
    {
        await SetAsync(GetId(), key, value, absoluteExpirationRelativeToNow);
    }

    public async Task SetAsync<T>(string id, string key, T value, TimeSpan absoluteExpirationRelativeToNow)
    {
        if (key.IsNullOrEmptyOrWhiteSpace()) return;

        if (value is null)
        {
            await RemoveAsync(key);
            return;
        }

        string _key = LocalKey(id, key);

        Task[] tasks =
        [
            Task.Run(() =>
            {
                // Set in local cache
                _localCache.Set(_key, value, absoluteExpirationRelativeToNow);
            }),

            Task.Run(async () =>
            {
                // Set in remote session cache.
                SemaphoreSlim semaphore = _locks.GetOrAdd(_key, _ => new SemaphoreSlim(1, 1));
                await semaphore.WaitAsync();
                try
                {
                    await UpdateSource(id, _key, value, absoluteExpirationRelativeToNow);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
                finally
                {
                    semaphore.Release();
                }
            }),
        ];

        await Task.WhenAll(tasks);
    }

    public void RefreshSession()
    {
        RefreshSessionAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task RefreshSessionAsync()
    {
        await RefreshSessionAsync(GetId());
    }

    public async Task RefreshSessionAsync(string id)
    {
        DateTimeOffset utcNow = DateTimeOffset.UtcNow;

        long lastTrackingNo = _lastTrackingNoDic.GetOrAdd(id, 0);
        DateTimeOffset lastRefreshTime = _lastRefreshTimeDic.GetOrAdd(id, utcNow);
        double elapsedSeconds = (utcNow - lastRefreshTime).TotalSeconds;
        if (elapsedSeconds < 5)
        {
            return;
        }

        Task[] tasks =
        [
            RefreshLocalSessionFromSource(id, lastTrackingNo, utcNow),
            RefreshSessionInSource(id, utcNow, _absoluteExpirationRelativeToNow)
        ];
        await Task.WhenAll(tasks);

        // Update last checked time
        _lastRefreshTimeDic[id] = utcNow;
    }

    public void Remove(string key)
    {
        RemoveAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task RemoveAsync(string key)
    {
        await RemoveAsync(GetId(), key);
    }

    public async Task RemoveAsync(string id, string key)
    {
        string _key = LocalKey(id, key);

        Task[] tasks =
        [
            Task.Run(() =>
            {
                // Remove from local cache
                _localCache.Remove(_key);
            }),

            Task.Run(async () => 
            {
                SemaphoreSlim semaphore = _locks.GetOrAdd(_key, _ => new SemaphoreSlim(1, 1));
                await semaphore.WaitAsync();
                try
                {
                    await RemoveFromSource(id, _key);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
                finally
                {
                    semaphore.Release();
                }
            }),
        ];

        await Task.WhenAll(tasks);
    }

    public void Clear()
    {
        ClearAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task ClearAsync()
    {
        await ClearAsync(GetId());
    }

    public async Task ClearAsync(string id)
    {
        Task[] tasks =
        [
            Task.Run(async () =>
            {
                // Remove all local cache keys for this session.
                var keys = await GetKeysAsync(id);
                Parallel.ForEach(keys, (key, cancel) =>
                {
                    _localCache.Remove(key);
                });
            }),

            Task.Run(async () =>
            {
                // Lock all keys
                IEnumerable<KeyValuePair<string, SemaphoreSlim>> locks = _locks.Where(x => x.Key.StartsWith(id));
                foreach (var l in locks)
                {
                    await l.Value.WaitAsync();
                }

                // Clear remote session cache.
                try
                {
                    await ClearFromSource(id);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
                finally
                {
                    // Release all keys
                    foreach (var l in locks)
                    {
                        l.Value.Release();
                    }
                }
            }),
        ];

        await Task.WhenAll(tasks);
    }

    public void Delete()
    {
        DeleteAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task DeleteAsync()
    {
        await DeleteAsync(GetId());
    }

    public async Task DeleteAsync(string id)
    {
        Task[] tasks =
        [
            Task.Run(async () =>
            {
                // Remove all local cache keys for this session.
                var keys = await GetKeysAsync(id);
                Parallel.ForEach(keys, (key, cancel) =>
                {
                    _localCache.Remove(key);
                });
            }),

            Task.Run(async () =>
            {
                // Lock all keys
                IEnumerable<KeyValuePair<string, SemaphoreSlim>> locks = _locks.Where(x => x.Key.StartsWith(id));
                foreach (var l in locks)
                {
                    await l.Value.WaitAsync();
                }

                // Delete remote session cache.
                try
                {
                    await DeleteSessionFromSource(id);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
                finally
                {
                    // Delete all keys
                    foreach (var l in locks)
                    {
                        _locks.TryRemove(l);
                    }
                }
            }),
        ];

        await Task.WhenAll(tasks);
    }

    public void SetDefaultSlidingExpiration(TimeSpan slidingExpiration)
    {
        AbsoluteExpirationRelativeToNow = slidingExpiration;
    }
}
