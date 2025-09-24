using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR.Client;
using NetCore_SqlServerHybridCache.Shared.ConfigOptions;
using System.Diagnostics;

namespace NetCore_SqlServerHybridCache.Services.Extensions;

public static class CacheNotificationHubExtension
{
    private static int connectionAttempts = 10;

    public static IServiceCollection AddCacheNotificationHub(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionHub = new HubConnectionBuilder()
        .WithUrl(configuration["SignalR:CacheHubUrl"] ?? string.Empty)
        .WithAutomaticReconnect()
        .Build();

        services.Configure<AppHubConnectionOptions>(options => options.CacheNotificationHubConnection = connectionHub);
        ConnectToCacheHub(connectionHub);

        // IMPORTANT: In-Memory cache will not work here. This need to be used after WebApplication app is created.
        // This can go in any other place where we want to listen to cache update notifications
        // We would typically put this in a service that needs to respond to cache updates
        //  And can access the HubConnection via dependency injection (AppHubConnectionOptions.CacheNotificationHubConnection)
        //connectionHub.On<string>("CacheUpdated", (key) =>
        //{
        //    // Handle cache update notifications here
        //    Debug.WriteLine($"Cache updated: {key}");
        //});

        return services;
    }

    private static void ConnectToCacheHub(HubConnection connectionHub)
    {
        try
        {
            connectionHub.StartAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            if (connectionAttempts > 0)
            {
                connectionAttempts--;
                Debug.WriteLine($"Failed to connect to CacheNotificationHub. Attempts left: {connectionAttempts}. Exception: {ex.Message}");
                Thread.Sleep(1000); // Wait for 2 seconds before retrying
                ConnectToCacheHub(connectionHub);
            }
        }
    }
}

