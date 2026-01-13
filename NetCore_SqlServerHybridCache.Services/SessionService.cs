using Microsoft.AspNetCore.DataProtection.KeyManagement;
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

    private readonly ConcurrentDictionary<string, List<TrackingItem>> _lastTrackingNoDic = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastRefreshTimeDic = new();

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

        int secondsToExpire = _configuration.GetValue("SessionTimeToExpireInSeconds", 1800);
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

    private static string LocalKey(string id, string key) => $"{id}-{key}";

    private async Task LogError(string? message, string? detail)
    {
        DynamicParameters dynParams = new DynamicParameters();
        dynParams.Add("@ErrorMessage", message);
        dynParams.Add("@ErrorDetail", detail);

        const string sql = "dbo.usp_logError";
        SqlConnection connection = GetConnection();
        await connection.ExecuteAsync(sql, dynParams, commandType: CommandType.StoredProcedure);
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
        IEnumerable<string> list =
            await connection.QueryAsync<string>(sql, dynParams, commandType: CommandType.StoredProcedure);

        return list;
    }

    private async Task<IEnumerable<SessionChange>> GetSessionChangesFromSource(string id, string trackingListJson)
    {
        DynamicParameters dynParams = new DynamicParameters();
        dynParams.Add("@Id", id);
        dynParams.Add("@TrackingListJson", trackingListJson);

        const string sql = "dbo.usp_getSessionChanges";
        using IDbConnection connection = GetConnection();
        IEnumerable<SessionChange> changes =
            await connection.QueryAsync<SessionChange>(sql, dynParams, commandType: CommandType.StoredProcedure);

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
        dynParams.Add("@UtcNow", DateTime.UtcNow);

        const string sql = "dbo.usp_getSessionValue";
        using IDbConnection connection = GetConnection();
        byte[]? bytes =
            await connection.QueryFirstOrDefaultAsync<byte[]>(sql, dynParams, commandType: CommandType.StoredProcedure);
        if (bytes is null || bytes.Length == 0)
        {
            return default(T);
        }

        using MemoryStream ms = new(bytes);
        T? result = await JsonSerializer.DeserializeAsync<T>(ms, cancellationToken: token);

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
        dynParams.Add("@AbsoluteExpiration", DateTime.UtcNow.Add(expirationTime));
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

        const string sql = "dbo.usp_deleteSessionValue";
        IDbConnection connection = GetConnection();
        await connection.ExecuteAsync(sql, dynParams, commandType: CommandType.StoredProcedure);
    }

    private async Task ClearFromSource(string id)
    {
        DynamicParameters dynParams = new DynamicParameters();
        dynParams.Add("@Id", id);

        const string sql = "dbo.usp_clearSession";
        IDbConnection connection = GetConnection();
        await connection.ExecuteAsync(sql, dynParams, commandType: CommandType.StoredProcedure);
    }

    private async Task DeleteSessionFromSource(string id)
    {
        DynamicParameters dynParams = new DynamicParameters();
        dynParams.Add("@Id", id);

        const string sql = "dbo.usp_deleteSessionCache";
        IDbConnection connection = GetConnection();
        await connection.ExecuteAsync(sql, dynParams, commandType: CommandType.StoredProcedure);
    }

    private async Task RefreshLocalSessionFromSource(string id)
    {
        Debug.WriteLine($"Instance: {_configuration.GetValue<string>("InstanceName")}");

        try
        {
            List<TrackingItem> trackingList = _lastTrackingNoDic.GetOrAdd(id, new List<TrackingItem>());
            string trackingListJson = JsonSerializer.Serialize(trackingList);

            // Get current version from remote cache
            IEnumerable<SessionChange> remoteChanges = await GetSessionChangesFromSource(id, trackingListJson);
            if (remoteChanges.Any())
            {
                // Remote session has changes. Update local cache for this session.
                Parallel.ForEach(remoteChanges, async (change) =>
                {
                    string localKey = LocalKey(change.SessionId, change.SessionKey);

                    if (change.SessionValue is null || change.SessionValue.Length == 0)
                    {
                        _localCache.Remove(localKey);
                        return;
                    }

                    if (change.DataType.IsNullOrEmptyOrWhiteSpace()) return;
                    Type? type = Type.GetType(change.DataType);
                    if (type == null)
                    {
                        type = AppDomain.CurrentDomain.GetAssemblies()
                            .Select(a => a.GetType(change.DataType, false, true))
                            .FirstOrDefault(t => t is not null);
                    }
                    if (type is null) return;

                    using MemoryStream ms = new(change.SessionValue);
                    ms.Position = 0;
                    object? result = await JsonSerializer.DeserializeAsync(ms, type);
                    if (result is null)
                    {
                        return;
                    }

                    _localCache.Set(localKey, result);

                    // Update last tracking no
                    TrackingItem? trackingItem = trackingList.FirstOrDefault(t => t.Key == change.SessionKey);
                    if (trackingItem is null)
                    {
                        trackingList.Add(new TrackingItem
                        {
                            Key = change.SessionKey,
                            TrackingNo = change.TrackingNo,
                        });
                    }
                    else
                    {
                        trackingItem.TrackingNo = change.TrackingNo;
                    }
                });
            }
        }
        catch (Exception e)
        {
            await LogError(e.Message, e.StackTrace);
        }
    }

    private async Task RefreshSessionInSource(string id, DateTime utcNow, TimeSpan expirationTime)
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
        (bool result, T? value) response = await TryGetAsync<T>(key);
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
        Debug.WriteLine($"Instance: {_configuration.GetValue<string>("InstanceName")}");
        Debug.WriteLine($"LastTrackingNo: {JsonSerializer.Serialize(_lastTrackingNoDic[id])}");

        try
        {
            string localKey = LocalKey(id, key);

            // Try local cache first
            (bool Success, T? Value) _localRes = _localCache.TryGet<T>(localKey);
            if (_localRes.Success && _localRes.Value != null)
            {
                return _localRes;
            }

            // Try remote cache
            T? remoteVal = await GetFromSourceAsync<T>(id, key, CancellationToken.None);
            if (remoteVal == null) return (false, default(T));

            // Save it in local cache for next use
            _localCache.Set(localKey, remoteVal);
            return (true, remoteVal);
        }
        catch (Exception e)
        {
            await LogError(e.Message, e.StackTrace);
            return (false, default(T));
        }
    }

    public IEnumerable<string> GetKeys()
    {
        return GetKeysAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task<IEnumerable<string>> GetKeysAsync()
    {
        try
        {
            string id = GetId();
            return await GetKeysAsync(id);
        }
        catch (Exception e)
        {
            await LogError(e.Message, e.StackTrace);
            return [];
        }
    }

    public async Task<IEnumerable<string>> GetKeysAsync(string id)
    {
        try
        {
            return await GetSessionKeyListFromSource(id);
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
            string id = GetId();
            bool keyExists = await SessionKeyExistsInSource(id, key);

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
        try
        {
            if (key.IsNullOrEmptyOrWhiteSpace()) return;

            var list = _lastTrackingNoDic.GetOrAdd(id, new List<TrackingItem>());
            if (!list.Any(t => t.Key == key))
            {
                list.Add(new TrackingItem { Key = key, TrackingNo = 0 });
            }

            if (value is null)
            {
                await RemoveAsync(key);
                return;
            }

            string localKey = LocalKey(id, key);

            Task[] tasks =
            [
                Task.Run(() =>
                {
                    // Set in local cache
                    _localCache.Set(localKey, value, absoluteExpirationRelativeToNow);
                }),

                Task.Run(async () =>
                {
                    // Set in remote session cache.
                    SemaphoreSlim semaphore = _locks.GetOrAdd(localKey, _ => new SemaphoreSlim(1, 1));
                    await semaphore.WaitAsync();
                    try
                    {
                        await UpdateSource(id, key, value, absoluteExpirationRelativeToNow);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }),
            ];

            await Task.WhenAll(tasks);
        }
        catch (Exception e)
        {
            await LogError(e.Message, e.StackTrace);
        }
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
        try
        {
            DateTime utcNow = DateTime.UtcNow;
            DateTime lastRefreshTime = _lastRefreshTimeDic.GetOrAdd(id, utcNow.AddSeconds(-10));
            double elapsedSeconds = (utcNow - lastRefreshTime).TotalSeconds;
            if (elapsedSeconds < 5)
            {
                return;
            }

            Task[] tasks =
            [
                RefreshLocalSessionFromSource(id),
                RefreshSessionInSource(id, utcNow, _absoluteExpirationRelativeToNow)
            ];
            await Task.WhenAll(tasks);

            // Update last checked time
            _lastRefreshTimeDic[id] = utcNow;
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
        await RemoveAsync(GetId(), key);
    }

    public async Task RemoveAsync(string id, string key)
    {
        try
        {
            string localKey = LocalKey(id, key);

            Task[] tasks =
            [
                Task.Run(() =>
                {
                    // Remove from local cache
                    _localCache.Remove(localKey);
                }),

                Task.Run(async () =>
                {
                    SemaphoreSlim semaphore = _locks.GetOrAdd(localKey, _ => new SemaphoreSlim(1, 1));
                    await semaphore.WaitAsync();
                    try
                    {
                        await RemoveFromSource(id, key);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }),
            ];

            await Task.WhenAll(tasks);
        }
        catch (Exception e)
        {
            await LogError(e.Message, e.StackTrace);
        }
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
        try
        {
            Task[] tasks =
            [
                Task.Run(async () =>
                {
                    // Remove all local cache keys for this session.
                    IEnumerable<string> keys = await GetKeysAsync(id);
                    
                    Parallel.ForEach(keys, (key) => 
                    { 
                        string localKey = LocalKey(id, key);
                        _localCache.Remove(localKey); 
                    });
                }),

                Task.Run(async () =>
                {
                    // Lock all keys
                    IEnumerable<KeyValuePair<string, SemaphoreSlim>> locks = 
                        _locks.Where(x => x.Key.StartsWith(id));
                    foreach (KeyValuePair<string, SemaphoreSlim> l in locks)
                    {
                        await l.Value.WaitAsync();
                    }

                    // Clear remote session cache.
                    try
                    {
                        await ClearFromSource(id);
                    }
                    finally
                    {
                        // Release all keys
                        foreach (KeyValuePair<string, SemaphoreSlim> l in locks)
                        {
                            l.Value.Release();
                        }
                    }
                }),
            ];

            await Task.WhenAll(tasks);
        }
        catch (Exception e)
        {
            await LogError(e.Message, e.StackTrace);
        }
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
        try
        {
            Task[] tasks =
            [
                Task.Run(async () =>
                {
                    // Remove all local cache keys for this session.
                    IEnumerable<string> keys = await GetKeysAsync(id);
                    Parallel.ForEach(keys, (key) => 
                    { 
                        string localKey = LocalKey(id, key);
                        _localCache.Remove(localKey); 
                    });
                }),

                Task.Run(async () =>
                {
                    // Lock all keys
                    IEnumerable<KeyValuePair<string, SemaphoreSlim>> locks =
                        _locks.Where(x => x.Key.StartsWith(id));
                    foreach (KeyValuePair<string, SemaphoreSlim> l in locks)
                    {
                        await l.Value.WaitAsync();
                    }

                    // Delete remote session cache.
                    try
                    {
                        await DeleteSessionFromSource(id);
                    }
                    finally
                    {
                        // Delete all keys
                        foreach (KeyValuePair<string, SemaphoreSlim> l in locks)
                        {
                            _locks.TryRemove(l);
                        }
                    }
                }),
            ];

            await Task.WhenAll(tasks);
        }
        catch (Exception e)
        {
            await LogError(e.Message, e.StackTrace);
        }
    }

    public void SetDefaultSlidingExpiration(TimeSpan slidingExpiration)
    {
        AbsoluteExpirationRelativeToNow = slidingExpiration;
    }
}