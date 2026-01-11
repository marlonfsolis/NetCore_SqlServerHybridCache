namespace NetCore_SqlServerHybridCache.Services.Models;

public record SessionChange
{
    public string SessionId { get; init; } = string.Empty;
    public string SessionKey { get; init; } = string.Empty;
    public byte[]? SessionValue { get; init; } = [];
    public long TrackingNo { get; init; } = 0;
    public string DataType { get; init; } = string.Empty;
}
