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
        ICacheService cache)
    {
        //// Get-Set the App Session ID
        //GetSetAppSessionId(context);

        Debug.WriteLine("Refreshing local cache in middleware...");

        //string sessionId = (string)context.Items[ContextItemsKeys.QuantumSessionId] ?? string.Empty;
        await cache.RefreshLocalCacheFromSource();
        //await cache.RefreshSessionAsync(sessionId);

        await _next(context);
    }
}
