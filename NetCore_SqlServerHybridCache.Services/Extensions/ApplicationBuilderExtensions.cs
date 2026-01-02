using Microsoft.AspNetCore.Builder;
using NetCore_SqlServerHybridCache.Services.Middlewares;

namespace NetCore_SqlServerHybridCache.Services.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseHybridCacheStore(this IApplicationBuilder app)
    {
        app.UseMiddleware<HybridCacheMiddleware>();

        return app;
    }
}
