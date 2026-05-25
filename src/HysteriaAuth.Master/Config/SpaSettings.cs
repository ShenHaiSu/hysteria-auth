namespace HysteriaAuth.Master.Config;

public class SpaSettings
{
    public const string SectionName = "Spa";

    public bool Enabled { get; set; } = true;
    public string StaticFilesPath { get; set; } = "wwwroot";
    public string FallbackFile { get; set; } = "index.html";
    public int CacheMaxAgeSeconds { get; set; } = 86400;
}
