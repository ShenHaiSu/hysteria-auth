namespace HysteriaAuth.Master.Models.DTOs;

public class UserDto
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public long TotalTrafficBytes { get; set; }
    public long UsedTrafficBytes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? AllowedNodes { get; set; }
    public string? Remark { get; set; }
}

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Email { get; set; }
    public long TotalTrafficBytes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    public List<string>? AllowedNodes { get; set; }
    public string? Remark { get; set; }
}

public class UpdateUserRequest
{
    public string? Email { get; set; }
    public long? TotalTrafficBytes { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public List<string>? AllowedNodes { get; set; }
    public string? Remark { get; set; }
    public string? Password { get; set; }
}

public class UserListResponse
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<UserDto> Items { get; set; } = new();
}
