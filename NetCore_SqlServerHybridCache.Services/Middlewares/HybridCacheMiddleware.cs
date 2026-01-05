using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace NetCore_SqlServerHybridCache.Services.Middlewares;

public class HybridCacheMiddleware
{
    private readonly RequestDelegate _next;

    public HybridCacheMiddleware(
        RequestDelegate next)
    {
        _next = next;
    }


    /* Public Method section *********************************************************/

    public async Task Invoke(
        HttpContext context,
        ICacheService cache,
        ISessionService session)
    {
        Debug.WriteLine("Refreshing local cache in middleware...");

        await cache.RefreshLocalCacheFromSource();
        await session.RefreshSessionAsync();

        await _next(context);
    }
}
