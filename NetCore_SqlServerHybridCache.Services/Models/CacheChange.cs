namespace NetCore_SqlServerHybridCache.Services.Models;

public record CacheChange
{
    public string AppCacheKey { get; init; } = string.Empty;
    public byte[] CacheValue { get; init; } = [];
    public long TrackingNo { get; init; }
    public string DataType { get; init; } = string.Empty;
}
