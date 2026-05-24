namespace HysteriaAuth.Master.Models.DTOs;

public class AuditLogDto
{
    public long Id { get; set; }
    public long AdminId { get; set; }
    public string AdminName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string? TargetId { get; set; }
    public string? Detail { get; set; }
    public string ClientIp { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AuditLogListResponse
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<AuditLogDto> Items { get; set; } = new();
}

public class AuditLogQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public long? AdminId { get; set; }
    public string? Action { get; set; }
    public string? TargetType { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}
