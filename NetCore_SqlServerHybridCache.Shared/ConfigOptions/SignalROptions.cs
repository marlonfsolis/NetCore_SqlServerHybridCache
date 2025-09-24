namespace NetCore_SqlServerHybridCache.Shared.ConfigOptions;

public class SignalROptions
{
    public string NotificationHubBaseUrl { get; set; } = string.Empty;
    public string CacheHubUrl { get; set; } = string.Empty;
}
