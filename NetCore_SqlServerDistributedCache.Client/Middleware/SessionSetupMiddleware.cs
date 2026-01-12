using NetCore_SqlServerHybridCache.Services.Constants;
using NetCore_SqlServerHybridCache.Services.Extensions;

namespace NetCore_SqlServerDistributedCache.Client.Middleware;

public class SessionSetupMiddleware
{
    private readonly RequestDelegate _next;

    public SessionSetupMiddleware(
        RequestDelegate next)
    {
        _next = next;
    }

    private static void GetSetAppSessionId(HttpContext context)
    {
        if (!context.Request.Cookies.Any(x => x.Key == CacheLiterals.ApplicationSessionIdName))
        {
            string qSessionId = Guid.NewGuid().ToString();
            context.Response.Cookies.Append(CacheLiterals.ApplicationSessionIdName, qSessionId, new CookieOptions()
            {
                Secure = true,
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Strict
            });

            context.Items.Add(CacheLiterals.ApplicationSessionIdName, qSessionId);
        }
        else
        {
            string? sessionId = context.Request.Cookies[CacheLiterals.ApplicationSessionIdName];
            if(!sessionId.IsNullOrEmptyOrWhiteSpace())
            {
                context.Items.Add(CacheLiterals.ApplicationSessionIdName, sessionId);
            }
        }
    }

    public async Task Invoke(HttpContext context)
    {
        GetSetAppSessionId(context);

        await _next(context);
    }
}
