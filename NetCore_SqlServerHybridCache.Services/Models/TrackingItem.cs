namespace NetCore_SqlServerHybridCache.Services.Models;

public record TrackingItem
{
    public string Key { get; set; } = string.Empty;
    public long TrackingNo { get; set; }
}
