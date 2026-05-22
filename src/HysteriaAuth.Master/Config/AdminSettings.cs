namespace HysteriaAuth.Master.Config;

public class AdminSettings
{
    public const string SectionName = "Admin";

    public int MaxFailedLoginAttempts { get; set; } = 5;
    public int LockoutDurationMinutes { get; set; } = 15;
    public int AuditLogRetentionDays { get; set; } = 365;
    public string DefaultPassword { get; set; } = "admin123";
}
