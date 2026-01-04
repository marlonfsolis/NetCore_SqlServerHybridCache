namespace NetCore_SqlServerHybridCache.Services.Models;

public record CacheChange
{
    public string AppCacheKey { get; set; } = string.Empty;
    public byte[] CacheValue { get; set; } = Array.Empty<byte>();
    public long TrackingNo { get; set; } = 0;
    public string DataType { get; set; } = string.Empty;
}
