namespace NetCore_SqlServerHybridCache.Aspire.ApiService.Hubs;

public interface ICacheNotificationHub
{
    Task CacheUpdated(string cacheKey);
}
