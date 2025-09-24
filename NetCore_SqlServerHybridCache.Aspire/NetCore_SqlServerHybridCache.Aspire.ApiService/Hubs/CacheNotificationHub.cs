using Microsoft.AspNetCore.SignalR;

namespace NetCore_SqlServerHybridCache.Aspire.ApiService.Hubs;

public class CacheNotificationHub : Hub<ICacheNotificationHub>
{
    public static string HubName => "CacheNotificationHub";
    public static string HubUrl => $"/{HubName}";

    public Task NotifyCacheUpdated(string cacheKey)
    {
        return Clients.All.CacheUpdated(cacheKey);
    }

    public override Task OnConnectedAsync()
    {
        Debug.WriteLine($"Client connected: {Context.ConnectionId}");

        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Debug.WriteLine($"Client disconnected: {Context.ConnectionId}");

        return base.OnDisconnectedAsync(exception);
    }
}
