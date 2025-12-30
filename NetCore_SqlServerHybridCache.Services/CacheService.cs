using System.Data;
using System.Diagnostics;

namespace NetCore_SqlServerHybridCache.Services;

public class CacheService : ICacheService
{
    private readonly IConfiguration _configuration;
    private readonly string _prefix;
    private readonly string _connectionString;
    private TimeSpan _absoluteExpirationRelativeToNow;

    public CacheService(
        HybridCache cache, 
        IConfiguration configuration,
        string? prefix, 
        TimeSpan absoluteExpirationRelativeToNow)
    {
        _configuration = configuration;
        _prefix = prefix ?? string.Empty;
        _absoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
        _connectionString = GetConnectionString();
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

    private async Task<bool> CacheKeyExistsInSource(string key)
    {
        DynamicParameters dynParams = new DynamicParameters();
        dynParams.Add("@Key", key);

        const string sql = "dbo.usp_getCacheKeyExists";
        using IDbConnection connection = GetConnection();
        bool exists = await connection.QueryFirstAsync<bool>(sql, dynParams);

        return exists;
    }

    private async Task<T?> GetFromSourceAsync<T>(string key, CancellationToken token = default)
    {
        if(token.IsCancellationRequested)
        {
            return default(T);
        }

        DynamicParameters dynParams = new DynamicParameters();
        dynParams.Add("@Key", key);

        string sql = "dbo.usp_getCacheValue";
        using IDbConnection connection = GetConnection();
        byte[]? bytes = await connection.QueryFirstOrDefaultAsync<byte[]>(sql, dynParams);
        if (bytes is null || bytes.Length == 0)
        {
            return default(T);
        }

        using MemoryStream ms = new(bytes);
        T? result = JsonSerializer.Deserialize<T>(ms);

        return result;
    }

    private async Task UpdateSourceByKey(string key, object value, TimeSpan expirationTime)
    {
        using MemoryStream ms = new();
        await JsonSerializer.SerializeAsync(ms, value);

        DynamicParameters dynParams = new DynamicParameters();
        dynParams.Add("@Key", key);
        dynParams.Add("@Value", ms.ToArray());
        dynParams.Add("@Expiration", DateTimeOffset.UtcNow.Add(expirationTime));

        const string sql = "dbo.usp_setCacheValue";
        IDbConnection connection = GetConnection();
        await connection.ExecuteAsync(sql, dynParams, commandType: CommandType.StoredProcedure);
    }

    private async Task RemoveFromSource(string key)
    {
        DynamicParameters dynParams = new DynamicParameters();
        dynParams.Add("@Key", key);

        string sql = "usp_deleteCacheValue";
        IDbConnection connection = GetConnection();
        await connection.ExecuteAsync(sql, dynParams);
    }



    /* Public Method section *********************************************************/

    public async Task<bool> KeyExists(string key)
    {
        string _key = GetKey(key);

        bool keyExists = await CacheKeyExistsInSource(_key);

        return keyExists;
    }

    public async Task<T?> Get<T>(string key)
    {
        string _key = GetKey(key);
        var result = await _cache.GetOrCreateAsync(
            _key,
            async cancel => await GetFromSourceAsync<T>(_key, cancel),
            cancellationToken: default
        );

        return result;
    }

    public async Task Set(string key, object value)
    {
        await Set(key, value, AbsoluteExpirationRelativeToNow);
    }

    public async Task Set(string key, object value, TimeSpan expirationTime)
    {
        if (value is null) return;

        string _key = GetKey(key);

        await _cache.SetAsync(_key, value, new HybridCacheEntryOptions
        {
            Expiration = expirationTime,
        });
        //await RemoveLocal(key);

        await UpdateSourceByKey(_key, value, expirationTime);
    }

    public async Task Remove(string key)
    {
        string _key = GetKey(key);

        await _cache.RemoveAsync(_key);

        await RemoveFromSource(_key);
    }

    public async Task RemoveLocal(string key)
    {
        string _key = GetKey(key);
        await _cache.RemoveAsync(_key);
    }
}
