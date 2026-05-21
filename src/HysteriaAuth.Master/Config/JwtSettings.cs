namespace HysteriaAuth.Master.Config;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "hysteria-auth-master";
    public string Audience { get; set; } = "hysteria-auth-admin";
    public int ExpirationMinutes { get; set; } = 1440;
    public int RefreshWindowMinutes { get; set; } = 5;
}
