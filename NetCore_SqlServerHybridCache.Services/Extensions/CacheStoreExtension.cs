using Microsoft.Extensions.Options;
using NetCore_SqlServerHybridCache.Shared.ConfigOptions;
using Microsoft.Extensions.DependencyInjection;

namespace NetCore_SqlServerHybridCache.Services.Extensions;

public static class CacheStoreExtension
{
    public static IServiceCollection AddHybridCacheStore(this IServiceCollection services)
    {
        // Add Hybrid Caching services to the container.
        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                // Set default options for cache entries here
                Expiration = TimeSpan.FromHours(24)
            };
        });

        // Add the cache sp
        services.AddSingleton<ICacheService>(sp =>
        {
            HybridCache cache = sp.GetRequiredService<HybridCache>();
            IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
            string prefix = "App_";
            TimeSpan expiration = TimeSpan.FromMinutes(10);
            var connOption =
                sp.GetRequiredService<IOptions<AppHubConnectionOptions>>();

            ICacheService cacheService = new CacheService(cache, configuration, prefix, expiration, connOption);

            return cacheService;
        });

        return services;
    }
}
