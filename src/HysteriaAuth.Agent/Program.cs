using System.Net;
using System.Text.Json;
using HysteriaAuth.Agent.Models;
using HysteriaAuth.Agent.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================
// 加载 agent.json 配置
// ============================
var agentConfig = LoadAgentConfig("Config/agent.json");
builder.Services.AddSingleton(agentConfig);

// ============================
// Service 层注册
// ============================
builder.Services.AddSingleton<AuthProxy>();

// ============================
// 最小化配置：只监听认证代理端口
// ============================
builder.WebHost.UseUrls($"http://{agentConfig.AuthProxy.ListenAddress}:{agentConfig.AuthProxy.ListenPort}");

var app = builder.Build();

// ============================
// 认证代理端点 — POST /auth (Hysteria 原生协议)
// ============================
app.MapPost("/auth", async (HttpContext context, AuthProxy authProxy, ILogger<Program> logger) =>
{
    try
    {
        var request = await context.Request.ReadFromJsonAsync<HysteriaAuthRequest>();
        if (request == null)
        {
            logger.LogWarning("收到无效的认证请求体");
            context.Response.StatusCode = 400;
            return;
        }

        var result = await authProxy.HandleAuthAsync(request);

        if (result.Ok)
        {
            context.Response.StatusCode = 200;
            await context.Response.WriteAsJsonAsync(result);
        }
        else
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(result);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "处理认证请求时发生未预期异常");
        context.Response.StatusCode = 500;
    }
});

// ============================
// 健康检查端点
// ============================
app.MapGet("/health", () => Results.Ok(new
{
    Status = "healthy",
    NodeId = agentConfig.NodeId,
    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
    Checks = new Dictionary<string, object>
    {
        { "auth_proxy", "running" }
    }
}));

app.Run();

// ============================
// 加载 agent.json
// ============================
static AgentConfig LoadAgentConfig(string path)
{
    if (!File.Exists(path))
    {
        // 尝试从 appsettings.json 查找
        path = "appsettings.json";
    }

    if (File.Exists(path))
    {
        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<AgentConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        if (config != null) return config;
    }

    // 返回默认配置
    return new AgentConfig
    {
        NodeId = Guid.NewGuid().ToString(),
        NodeName = "Edge Node",
        MasterServerUrl = "https://master.example.com",
        AuthProxy = new AgentAuthProxyConfig
        {
            ListenAddress = "127.0.0.1",
            ListenPort = 8080
        }
    };
}
