using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using HysteriaAuth.Master.Config;
using HysteriaAuth.Master.Data;
using HysteriaAuth.Master.Middleware;
using HysteriaAuth.Master.Repositories;
using HysteriaAuth.Master.Services;
using INodeRepository = HysteriaAuth.Master.Repositories.INodeRepository;
using INodeStatusRepository = HysteriaAuth.Master.Repositories.INodeStatusRepository;

var builder = WebApplication.CreateBuilder(args);

// ============================
// 配置强类型绑定
// ============================
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<AdminSettings>(builder.Configuration.GetSection(AdminSettings.SectionName));
builder.Services.Configure<SpaSettings>(builder.Configuration.GetSection(SpaSettings.SectionName));

// ============================
// 数据库
// ============================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ============================
// JSON 序列化配置（DateTime → ISO 8601 UTC）
// ============================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });

// ============================
// HTTP 客户端工厂（用于 KickService）
// ============================
builder.Services.AddHttpClient();

// ============================
// Repository 层注册
// ============================
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IAuthLogRepository, AuthLogRepository>();
builder.Services.AddScoped<INodeRepository, NodeRepository>();
builder.Services.AddScoped<INodeStatusRepository, NodeStatusRepository>();
builder.Services.AddScoped<ITrafficRepository, TrafficRepository>();  // Phase 3
builder.Services.AddScoped<ISessionRepository, SessionRepository>();  // Phase 3
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// ============================
// AES 加密服务（节点密钥加密存储）
// ============================
var encryptionKey = builder.Configuration.GetValue<string>("Encryption:MasterKey")
    ?? Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
builder.Services.AddSingleton(new AesEncryptionService(encryptionKey));

// ============================
// Service 层注册
// ============================
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<NodeService>();
builder.Services.AddScoped<TrafficService>();    // Phase 3
builder.Services.AddScoped<KickService>();      // Phase 3
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<ConfigGeneratorService>();  // Phase 7: Hysteria 2 YAML 配置生成器

// ============================
// 后台服务（节点离线检测 + 数据保留策略）
// ============================
builder.Services.AddHostedService<NodeHealthCheckService>();
builder.Services.AddHostedService<DataRetentionService>(); // Phase 3

// ============================
// CORS 配置（仅管理 API 需要）
// ============================
builder.Services.AddCors(options =>
{
    var corsSection = builder.Configuration.GetSection("Cors");
    var origins = corsSection.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

    options.AddPolicy("AdminCors", policy =>
    {
        policy.WithOrigins(origins)
              .WithMethods("GET", "POST", "PUT", "DELETE")
              .WithHeaders("Authorization", "Content-Type")
              .WithExposedHeaders("X-Request-Id")
              .SetPreflightMaxAge(TimeSpan.FromSeconds(3600));
    });
});

var app = builder.Build();

// ============================
// 数据库自动迁移 + 种子数据
// ============================
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();  // 使用 Migration 以确保应用所有数据库变更（如 ExpandNodeTable）

    // 种子数据：创建默认 super_admin 账号
    var adminSettings = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AdminSettings>>().Value;
    var existingAdmin = await dbContext.Admins.FirstOrDefaultAsync(a => a.Username == "admin");
    if (existingAdmin == null)
    {
        dbContext.Admins.Add(new HysteriaAuth.Master.Models.Entities.Admin
        {
            Username = "admin",
            Password = BCrypt.Net.BCrypt.HashPassword(adminSettings.DefaultPassword, 12),
            Role = "super_admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }
}

// ============================
// 中间件管道（顺序敏感）
// ============================
app.UseGlobalExceptionHandler();    // 1. 全局异常处理（最外层）
app.UseCors("AdminCors");           // 2. CORS（必须在认证中间件之前，否则 OPTIONS 预检会被拦截）
app.UseNodeAuth();                  // 3. 节点密钥认证（/api/v1/auth/*, /api/v1/nodes/*）
app.UseJwtAuth();                   // 4. JWT 认证（/api/v1/admin/*, /api/v1/users/*）
app.UseAuditContext();              // 5. 审计上下文中间件（注入 ClientIp）

// ============================
// 5. SPA 静态文件托管（条件启用）
// ============================
var spaSettings = app.Services.GetRequiredService<
    Microsoft.Extensions.Options.IOptions<SpaSettings>>().Value;

string? spaAbsolutePath = null;
if (spaSettings.Enabled)
{
    var contentRoot = app.Environment.ContentRootPath;
    var rawPath = spaSettings.StaticFilesPath;
    spaAbsolutePath = Path.IsPathRooted(rawPath)
        ? rawPath
        : Path.GetFullPath(Path.Combine(contentRoot, rawPath));

    if (Directory.Exists(spaAbsolutePath))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(spaAbsolutePath),
            OnPrepareResponse = ctx =>
            {
                // 对带 hash 的静态资源设置长缓存
                var ext = Path.GetExtension(ctx.File.Name);
                if (ext is ".js" or ".css" or ".woff" or ".woff2"
                    or ".ttf" or ".svg" or ".png" or ".ico")
                {
                    ctx.Context.Response.Headers.CacheControl =
                        $"public, max-age={spaSettings.CacheMaxAgeSeconds}";
                }
            }
        });

        app.Logger.LogInformation(
            "SPA static files enabled: {Path} (resolved from '{RawPath}')",
            spaAbsolutePath, rawPath);
    }
    else
    {
        app.Logger.LogWarning(
            "SPA static files path not found: {Path} (resolved from '{RawPath}'). " +
            "Static file serving is disabled. Run 'npm run build' and place dist/* into this directory.",
            spaAbsolutePath, rawPath);
    }
}

app.MapControllers();               // 6. API 路由（必须在 MapFallbackToFile 之前）

// ============================
// 7. SPA 兜底路由（必须在 MapControllers 之后）
// ============================
if (spaSettings.Enabled && spaAbsolutePath != null && Directory.Exists(spaAbsolutePath))
{
    app.MapFallbackToFile(spaSettings.FallbackFile, new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(spaAbsolutePath)
    });
}

app.Run();
