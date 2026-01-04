namespace NetCore_SqlServerHybridCache.Services.Models;

public record SessionChange
{
    public string SessionId { get; set; } = string.Empty;
    public string SessionKey { get; set; } = string.Empty;
    public byte[] SessionValue { get; set; } = Array.Empty<byte>();
    public long TrackingNo { get; set; } = 0;
    public string DataType { get; set; } = string.Empty;
}
