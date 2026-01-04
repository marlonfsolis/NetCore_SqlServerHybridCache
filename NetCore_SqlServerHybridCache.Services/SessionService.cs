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

    // _lastTrackingNo
    private ConcurrentDictionary<string, long> _lastTrackingNoDic = new();

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
        bool exists = await connection.QueryFirstAsync<bool>(sql, dynParams);

        return exists;
    }

    private async Task<IEnumerable<string>> GetSessionKeyListFromSource(string id)
    {
        DynamicParameters dynParams = new DynamicParameters();
        dynParams.Add("@Id", id);

        const string sql = "dbo.usp_getSessionKeyList";
        using IDbConnection connection = GetConnection();
        IEnumerable<string> list = await connection.QueryAsync<string>(sql, dynParams);

        return list;
    }

    private async Task<IEnumerable<SessionChange>> GetSessionChangesFromSource(string id, long lastTrackingNo)
    {
        DynamicParameters dynParams = new DynamicParameters();
        dynParams.Add("@Id", id);
        dynParams.Add("@LastTrackingNo", lastTrackingNo);

        const string sql = "dbo.usp_getSessionChanges";
        using IDbConnection connection = GetConnection();
        IEnumerable<SessionChange> changes = await connection.QueryAsync<SessionChange>(sql, dynParams);

        return changes;
    }



    /* Public Method section *********************************************************/


    public void CreateSession()
    {
        throw new NotImplementedException();
    }

    public Task CreateSessionAsync()
    {
        throw new NotImplementedException();
    }

    public Task CreateSessionAsync(string id)
    {
        throw new NotImplementedException();
    }

    public TItem? Get<TItem>(string key)
    {
        throw new NotImplementedException();
    }

    public Task<TItem?> GetAsync<TItem>(string key)
    {
        throw new NotImplementedException();
    }

    public (bool result, TItem? value) TryGet<TItem>(string key)
    {
        throw new NotImplementedException();
    }

    public Task<(bool result, TItem? value)> TryGetAsync<TItem>(string key)
    {
        throw new NotImplementedException();
    }

    public Task<(bool result, TItem? value)> TryGetAsync<TItem>(string id, string key)
    {
        throw new NotImplementedException();
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

    public void Set(string key, object value)
    {
        throw new NotImplementedException();
    }

    public void Set(string key, object value, TimeSpan absoluteExpirationRelativeToNow)
    {
        throw new NotImplementedException();
    }

    public Task SetAsync(string key, object value)
    {
        throw new NotImplementedException();
    }

    public Task SetAsync(string key, object value, TimeSpan absoluteExpirationRelativeToNow)
    {
        throw new NotImplementedException();
    }

    public Task SetAsync(string id, string key, object value, TimeSpan absoluteExpirationRelativeToNow)
    {
        throw new NotImplementedException();
    }

    public void RefreshSession()
    {
        throw new NotImplementedException();
    }

    public Task RefreshSessionAsync()
    {
        throw new NotImplementedException();
    }

    public Task RefreshSessionAsync(string id)
    {
        throw new NotImplementedException();
    }

    public void Remove(string key)
    {
        throw new NotImplementedException();
    }

    public Task RemoveAsync(string key)
    {
        throw new NotImplementedException();
    }

    public Task RemoveAsync(string id, string key)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public Task ClearAsync()
    {
        throw new NotImplementedException();
    }

    public Task ClearAsync(string id)
    {
        throw new NotImplementedException();
    }

    public void Delete()
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync()
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(string id)
    {
        throw new NotImplementedException();
    }

    public void SetDefaultSlidingExpiration(TimeSpan slidingExpiration)
    {
        throw new NotImplementedException();
    }
}
