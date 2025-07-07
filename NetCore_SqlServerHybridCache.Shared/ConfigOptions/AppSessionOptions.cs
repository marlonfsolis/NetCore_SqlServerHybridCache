using System.Net;

namespace NetCore_SqlServerDistributedCache.Shared.ConfigOptions;

public class AppSessionOptions
{
    public const string SessionName = "AppSession";

    public required ulong SessionIdleTimeout { get; set; }
    public required bool Cookie_HttpOnly { get; set; }
    public required bool Cookie_IsEssential { get; set; }

}
