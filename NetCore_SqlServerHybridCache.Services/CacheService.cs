using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using NetCore_SqlServerHybridCache.Shared.ConfigOptions;
using System.Diagnostics;

namespace NetCore_SqlServerHybridCache.Services;

public class CacheService : ICacheService
{
    private readonly HybridCache _cache;
    private readonly IConfiguration _configuration;
    private readonly AppHubConnectionOptions _appHubConnectionOption;
    private string _prefix;
    private TimeSpan _absoluteExpirationRelativeToNow;
    private SqlConnection _connection;

    public CacheService(
        HybridCache cache, 
        IConfiguration configuration,
        string? prefix, 
        TimeSpan absoluteExpirationRelativeToNow,
        IOptions<AppHubConnectionOptions> appHubConnectionOption)
    {
        _cache = cache;
        _configuration = configuration;
        _prefix = prefix ?? string.Empty;
        _absoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
        _appHubConnectionOption = appHubConnectionOption.Value;
        _connection = new SqlConnection();

        SetupConnection();
    }


    /* Property section *********************************************************/

    public string Prefix
    {
        get { return _prefix; }
        set { _prefix = value ?? string.Empty; }
    }

    public TimeSpan AbsoluteExpirationRelativeToNow
    {
        get { return _absoluteExpirationRelativeToNow; }
        set { _absoluteExpirationRelativeToNow = value; }
    }


    /* Private Method section *********************************************************/

    private void SetupConnection()
    {
        string? connectionString = _configuration.GetConnectionString("HybridCache");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString));
        }

        _connection = new SqlConnection(connectionString);
        if (_connection is null)
        {
            throw new ArgumentNullException(nameof(_connection));
        }
    }


    private async Task<bool> CacheKeyExistsInSource(string key)
    {
        DynamicParameters dynParams = new DynamicParameters();
        dynParams.Add("@Key", key);

        string sql = "SELECT ac.Id FROM AppCache ac WHERE ac.Id = @Key;";
        string? keyResult = await _connection.QueryFirstOrDefaultAsync<string>(sql, dynParams);

        return !string.IsNullOrEmpty(keyResult);
    }

    private async Task<T?> GetFromSourceAsync<T>(string key, CancellationToken token = default)
    {
        if(token.IsCancellationRequested)
        {
            return default(T);
        }

        DynamicParameters dynParams = new DynamicParameters();
        dynParams.Add("@Key", key);

        string sql = "SELECT [Value] FROM AppCache ac WHERE ac.Id = @Key AND ac.AbsoluteExpiration > GETDATE();";
        byte[]? bytes = await _connection.QueryFirstOrDefaultAsync<byte[]>(sql, dynParams);
        if (bytes is null)
        {
            return default(T);
        }

        using MemoryStream ms = new(bytes);
        var result = JsonSerializer.Deserialize<T>(ms);

        return result;
    }

    private async Task UpdateSourceByKey(string key, object value, TimeSpan expirationTime)
    {
        using MemoryStream ms = new();
        JsonSerializer.Serialize(ms, value);

        DynamicParameters dynParams = new DynamicParameters();
        dynParams.Add("@Key", key);
        dynParams.Add("@Value", ms.ToArray());
        dynParams.Add("@Expiration", DateTimeOffset.UtcNow.Add(expirationTime));

        string sql = "DELETE [HybridCache].[dbo].[AppCache] WHERE Id = @Key;INSERT INTO [HybridCache].[dbo].[AppCache] VALUES (@Key, @Value, @Expiration);";
        await _connection.ExecuteAsync(sql, dynParams);
    }

    private async Task RemoveFromSource(string key)
    {
        DynamicParameters dynParams = new DynamicParameters();
        dynParams.Add("@Key", key);

        string sql = "DELETE [HybridCache].[dbo].[AppCache] WHERE Id = @Key";
        await _connection.ExecuteAsync(sql, dynParams);
    }

    private async Task NotifyCacheUpdate(string key)
    {
        if (_appHubConnectionOption.CacheNotificationHubConnection is not null)
        {
            try
            {
                await _appHubConnectionOption.CacheNotificationHubConnection.InvokeAsync("NotifyCacheUpdate", key);
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log the error)
                Debug.WriteLine($"Error notifying cache update: {ex.Message}");
            }
        }
    }



    /* Public Method section *********************************************************/

    public async Task<bool> KeyExists(string key)
    {
        string _key = $"{Prefix}{key}";

        bool keyExists = await CacheKeyExistsInSource(_key);

        return keyExists;
    }

    public async Task<T?> Get<T>(string key)
    {
        string _key = $"{Prefix}{key}";
        var result = await _cache.GetOrCreateAsync(
            _key,
            async cancel => await GetFromSourceAsync<T>(_key, cancel),
            cancellationToken: default
        );

        return result;
    }

    public async Task Set(string key, object value)
    {
        await Set(key, value, _absoluteExpirationRelativeToNow);
    }

    public async Task Set(string key, object value, TimeSpan expirationTime)
    {
        if (value is null) return;

        string _key = $"{Prefix}{key}";

        await _cache.SetAsync(_key, value, new HybridCacheEntryOptions
        {
            Expiration = expirationTime,
        });
        //await RemoveLocal(key);

        await UpdateSourceByKey(_key, value, expirationTime);

        // Notify other instances to remove the key from their local cache
        await NotifyCacheUpdate(key);
    }

    public async Task Remove(string key)
    {
        string _key = $"{Prefix}{key}";

        await _cache.RemoveAsync(_key);

        await RemoveFromSource(_key);
    }

    public async Task RemoveLocal(string key)
    {
        string _key = $"{Prefix}{key}";
        await _cache.RemoveAsync(_key);
    }
}
