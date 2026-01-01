using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace NetCore_SqlServerHybridCache.Services.Extensions;

public static class CacheStoreExtension
{
    public static IServiceCollection AddHybridCacheStore(this IServiceCollection services)
    {
        services.AddSingleton<IAppMemoryCache, AppMemoryCache>();

        // Register Hybrid Cache Service
        services.AddSingleton<ICacheService>(sp =>
        {
            IAppMemoryCache localCache = sp.GetRequiredService<IAppMemoryCache>();
            IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
            string prefix = "App_";
            TimeSpan expiration = TimeSpan.FromMinutes(10);

            ICacheService cacheService = new CacheService(localCache, configuration, prefix, expiration);

            return cacheService;
        });

        return services;
    }
}
