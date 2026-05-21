using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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
// Repository 层注册
// ============================
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IAuthLogRepository, AuthLogRepository>();
builder.Services.AddScoped<INodeRepository, NodeRepository>();
builder.Services.AddScoped<INodeStatusRepository, NodeStatusRepository>();
builder.Services.AddScoped<ITrafficRepository, TrafficRepository>();  // Phase 3
builder.Services.AddScoped<ISessionRepository, SessionRepository>();  // Phase 3

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
    dbContext.Database.EnsureCreated();

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
app.UseNodeAuth();                  // 2. 节点密钥认证（/api/v1/auth/*, /api/v1/nodes/*）
app.UseJwtAuth();                   // 3. JWT 认证（/api/v1/admin/*, /api/v1/users/*）

app.UseCors("AdminCors");           // 4. CORS

app.MapControllers();               // 5. 路由到控制器

app.Run();
