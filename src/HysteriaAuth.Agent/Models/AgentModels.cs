using System.Text.Json;

namespace HysteriaAuth.Agent.Models;

public class HysteriaAuthRequest
{
    public string Addr { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public ulong Tx { get; set; }
}

public class HysteriaAuthResponse
{
    public bool Ok { get; set; }
    public string Id { get; set; } = string.Empty;
}

public class InternalAuthRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string ClientIp { get; set; } = string.Empty;
}

public class InternalAuthResponse
{
    public bool Success { get; set; }
    public long UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public long RemainingTraffic { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class AgentConfig
{
    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string MasterServerUrl { get; set; } = string.Empty;
    public string NodeSecret { get; set; } = string.Empty;
    public string AgentVersion { get; set; } = "1.0.0";
    public AgentAuthProxyConfig AuthProxy { get; set; } = new();
    public AgentLoggingConfig Logging { get; set; } = new();
}

public class AgentAuthProxyConfig
{
    public string ListenAddress { get; set; } = "127.0.0.1";
    public int ListenPort { get; set; } = 8080;
}

public class AgentLoggingConfig
{
    public string LogLevel { get; set; } = "Information";
    public string? File { get; set; }
}
