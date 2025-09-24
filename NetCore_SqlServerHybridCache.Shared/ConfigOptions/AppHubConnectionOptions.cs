using Microsoft.AspNetCore.SignalR.Client;

namespace NetCore_SqlServerHybridCache.Shared.ConfigOptions;

public class AppHubConnectionOptions
{
    public HubConnection? CacheNotificationHubConnection { get; set; }
}
