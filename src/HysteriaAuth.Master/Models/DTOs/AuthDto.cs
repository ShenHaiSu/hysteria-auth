namespace HysteriaAuth.Master.Models.DTOs;

public class AuthRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string ClientIp { get; set; } = string.Empty;
}

public class AuthResponse
{
    public bool Success { get; set; }
    public long UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public long RemainingTraffic { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class DashboardResponse
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalNodes { get; set; }
    public int ActiveNodes { get; set; }
    public int OnlineUsersNow { get; set; }
    public long TotalTrafficToday { get; set; }
    public long TotalTrafficThisMonth { get; set; }
}
