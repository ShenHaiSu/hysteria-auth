using System.Text.Json;
using HysteriaAuth.Agent.Models;
using HysteriaAuth.Agent.Services;

// ============================
// Phase 2: Edge Agent 完整启动序列
// ============================
// 启动顺序: 1. 加载配置 → 2. 令牌比对/注册 → 3. 启动所有模块 → 4. 发送首次心跳
// ============================

var builder = WebApplication.CreateBuilder(args);

// ============================
// 1. 初始化 — 加载配置 + 令牌比对 + 注册
// ============================
var configPath = args.Length > 0 && !args[0].StartsWith("--")
    ? args[0] : "Config/agent.json";

var loggerFactory = LoggerFactory.Create(cfg => cfg.AddConsole().SetMinimumLevel(LogLevel.Information));
var initLogger = loggerFactory.CreateLogger<Initializer>();
var initializer = new Initializer(configPath, initLogger);

var initSuccess = await initializer.InitializeAsync(args);
if (!initSuccess)
{
    Console.Error.WriteLine("[FATAL] Edge Agent 初始化失败，退出。");
    Environment.Exit(1);
}

var agentConfig = initializer.Config;
builder.Services.AddSingleton(agentConfig);
builder.Services.AddSingleton(initializer);  // Phase 7: StatusReporter 依赖 Initializer 进行配置同步

// ============================
// 2. 注册服务
// ============================
builder.Services.AddSingleton(agentConfig);
builder.Services.AddSingleton(initializer);
builder.Services.AddSingleton<SystemMonitor>();
builder.Services.AddSingleton<AuthProxy>();
builder.Services.AddHostedService<StatusReporter>();   // 心跳上报后台服务（同时作为单例供 TrafficCollector 注入）
builder.Services.AddHostedService<TrafficCollector>(); // Phase 3: 流量采集后台服务
builder.Services.AddHttpClient("TrafficStats", client =>
{
    // TrafficCollector 在运行时设置 BaseAddress，此处仅注册命名客户端
});

// ============================
// 3. 配置监听地址
// ============================
// 认证代理监听 localhost:8080（Hysteria 通过此端口调用 /auth）
builder.WebHost.UseUrls($"http://{agentConfig.AuthProxy.ListenAddress}:{agentConfig.AuthProxy.ListenPort}");

var app = builder.Build();

// ============================
// 4. 认证代理端点 — POST /auth (Hysteria 原生协议)
// ============================
app.MapPost("/auth", async (HttpContext context, AuthProxy authProxy) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
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
// 5. 健康检查端点 — GET /health
// ============================
app.MapGet("/health", () =>
{
    var checks = new Dictionary<string, object>
    {
        { "system_monitor", "running" },
        { "auth_proxy", "running" },
        { "master_reachable", "unknown" },
        { "hysteria_reachable", "unknown" },
        { "traffic_collector", "running" }      // Phase 3: 已启动
    };

    return Results.Ok(new
    {
        Status = "healthy",
        NodeId = agentConfig.NodeId,
        Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        Checks = checks
    });
});

// ============================
// 6. 踢用户下线端点 — POST /kick-user (Phase 3, 由主服务器 KickService 调用)
// ============================
app.MapPost("/kick-user", async (HttpContext context) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    try
    {
        var request = await context.Request.ReadFromJsonAsync<HysteriaAuth.Agent.Models.KickUserRequest>();
        if (request == null || string.IsNullOrWhiteSpace(request.Username))
        {
            context.Response.StatusCode = 400;
            return;
        }

        // 转发到 Hysteria POST /kick
        using var client = new HttpClient();
        var trafficBaseUrl = $"http://{agentConfig.TrafficStats.ListenAddress}:{agentConfig.TrafficStats.ListenPort}";
        var kickUrl = $"{trafficBaseUrl}/kick?secret={Uri.EscapeDataString(agentConfig.TrafficStats.Secret)}";
        var content = JsonContent.Create(new { username = request.Username });

        var response = await client.PostAsync(kickUrl, content);
        context.Response.StatusCode = response.IsSuccessStatusCode ? 200 : 502;
        logger.LogInformation("踢用户下线: {Username}, Hysteria响应: {Code}",
            request.Username, (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "踢用户下线失败: {Message}", ex.Message);
        context.Response.StatusCode = 502;
    }
});

// ============================
// 7. 启动应用
// ============================
app.Logger.LogInformation("Edge Agent 启动完成: NodeId={NodeId}, AuthProxy={Addr}:{Port}",
    agentConfig.NodeId,
    agentConfig.AuthProxy.ListenAddress,
    agentConfig.AuthProxy.ListenPort);

app.Run();
