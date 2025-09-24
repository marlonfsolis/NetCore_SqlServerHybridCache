using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using NetCore_SqlServerHybridCache.Shared.ConfigOptions;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

namespace NetCore_SqlServerHybridCache.Services.Extensions;

public static class UseNotificationHubEventsExtension
{
    public static IApplicationBuilder UseNotificationHubEvents(this WebApplication app)
    {
        var appHubConnectionOptions = app.Services.GetRequiredService<IOptions<AppHubConnectionOptions>>().Value;
        var connectionHub = appHubConnectionOptions.CacheNotificationHubConnection;
        if (connectionHub == null) throw new ArgumentNullException(nameof(connectionHub), "CacheNotificationHubConnection is not configured.");
        connectionHub.On<string>("CacheUpdated", (key) =>
        {
            // Handle cache update notifications here
            Debug.WriteLine($"Cache updated: {key}");

            var cache = app.Services.GetRequiredService<ICacheService>();
            cache.RemoveLocal(key);

            // CreateScope is used to require an scoped service inside a singleton service.
            // We do not really need to do this here.
            //using (var scope = app.Services.CreateScope())
            //{
            //    var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
            //    cache.RemoveLocal(key);
            //}
        });
        return app;
    }
}
