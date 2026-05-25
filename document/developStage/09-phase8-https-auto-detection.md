# Phase 8: HTTPS 证书自动检测与监听端口规范化

> **阶段**: Phase 8 | **预估工期**: 0.5~1 天 | **依赖**: Phase 6（SPA 集成部署：路径解析模式、SpaSettings 配置类模式）
>
> **来源文档**: [`document/architect/spa-integration.md`](../architect/spa-integration.md) §2 · [`../architect/security-design.md`](../architect/security-design.md) §1 · [`../architect/project-structure-configuration.md`](../architect/project-structure-configuration.md) §2 · [`../develop/backend-development-spec.md`](../develop/backend-development-spec.md) §6.2, §10.1, §12.1

---

## 1. 阶段目标与范围

### 1.1 总体目标

1. **监听端口规范化** — 在 `appsettings.json` 中明确定义 Master Server 监听的网卡地址和端口，默认 `0.0.0.0:5000`，使其可配置、可追溯，结束当前"隐式依赖环境变量/launchSettings"的模糊状态。

2. **HTTPS 证书自动检测** — 启动时检查 `cert/` 目录下的 `server.cert`（公钥）和 `server.key`（私钥），根据检测结果**在同一个端口上**决定使用 HTTPS 还是 HTTP：
   - 证书齐全 → 端口 `5000` 启用 **HTTPS**
   - 证书缺失 → 端口 `5000` 回退 **HTTP** + 输出安全警告

### 1.2 核心逻辑流程

```
启动 → 读取 appsettings.json 中 Https 配置节（含 ListenAddress、ListenPort）
     → 解析 CertDirectoryPath 为绝对路径
     → 检查 {absolutePath}/server.cert 是否存在
     → 检查 {absolutePath}/server.key 是否存在
     
     ┌─ 两者均存在 → Kestrel 在 {ListenAddress}:{ListenPort} 上启用 HTTPS
     │               → 日志 Information: "HTTPS enabled on https://{addr}:{port}..."
     │
     └─ 任一缺失   → Kestrel 在 {ListenAddress}:{ListenPort} 上使用 HTTP
                     → 日志 Warning: "HTTPS certificate not found... Running in HTTP mode - INSECURE!"
                     → 控制台输出醒目的安全警告
```

> **核心理念**：只使用**一个端口**。生产环境部署证书后自动变为 HTTPS，开发环境无证书时自动降级为 HTTP。运维人员只需关注同一个端口号，无需记忆两套端口。

### 1.3 范围清单

| 序号 | 交付项 | 说明 |
|------|--------|------|
| 8.1 | `HttpsSettings` 强类型配置类 | `ListenAddress`、`ListenPort`、`CertDirectoryPath`、`CertFileName`、`CertKeyFileName` |
| 8.2 | `appsettings.json` 新增 `Https` 配置节 | 含监听地址/端口 + 证书路径三个字段 |
| 8.3 | `Program.cs` 中监听端口规范化 + HTTPS 自动检测逻辑 | 单端口策略：路径解析 + 文件存在性检查 + Kestrel 条件配置 |
| 8.4 | 日志与控制台双重输出 | Information (HTTPS) / Warning (HTTP + 安全风险) |
| 8.5 | 架构文档同步更新 | `backend-development-spec.md`、`security-design.md`、`project-structure-configuration.md`、`deployment.md` |

---

## 2. 阶段启动前置检查

> Phase 8 依赖 Phase 6 完成的 SPA 路径解析模式和配置绑定模式。

| # | 检查项 | 验证内容 | 通过标准 |
|---|--------|----------|----------|
| P8.1 | `SpaSettings.cs` 配置类模式就绪 | 文件存在，绑定 `Spa` 配置节 | `Enabled`、`StaticFilesPath` 等属性可正常读取 |
| P8.2 | `appsettings.json` 中 `Spa` 节可用 | 启动后 `Spa.Enabled` 生效 | SPA 中间件正常工作 |
| P8.3 | `Program.cs` 中有路径解析模板代码 | SPA 的 `Path.IsPathRooted` + `Path.GetFullPath` + `Directory.Exists` 模式可复用 | SPA 路径解析日志正常输出 |
| P8.4 | Kestrel 当前监听方式不明确 | 检查 `launchSettings.json` 或 `ASPNETCORE_URLS` 环境变量 | 端口配置来源分散，亟需统一到 `appsettings.json` |
| P8.5 | 项目根目录下尚无 `cert/` 文件夹 | `ls cert/` 或 `dir cert` | 目录不存在（开发环境通常如此） |
| P8.6 | ✅ 阅读 [`spa-integration.md` §2](../architect/spa-integration.md#2-配置设计)（路径解析规则） | — | — |
| P8.7 | ✅ 阅读 [`security-design.md` §1](../architect/security-design.md#1-认证安全)（HTTPS 要求） | — | — |
| P8.8 | ✅ 阅读 [`backend-development-spec.md` §6.2](../develop/backend-development-spec.md#62-通信安全)（通信安全） | — | — |

---

## 3. 具体任务清单

### 第 1 天：配置类 + appsettings.json + Program.cs 改造 + 文档更新

#### 3.1 HttpsSettings 强类型配置类

- [ ] **8.1.1** 创建 [`Config/HttpsSettings.cs`](../../src/HysteriaAuth.Master/Config/HttpsSettings.cs)：

```csharp
namespace HysteriaAuth.Master.Config;

/// <summary>
/// HTTPS 证书自动检测与监听端口配置。
/// 启动时自动检查 CertDirectoryPath 目录下是否存在公钥/私钥文件，
/// 存在则在 ListenPort 上启用 HTTPS，否则在 ListenPort 上回退 HTTP 并输出安全警告。
/// </summary>
public class HttpsSettings
{
    public const string SectionName = "Https";

    /// <summary>
    /// Kestrel 监听的网卡地址。默认 "0.0.0.0"（监听所有网卡，IPv4 + IPv6）。
    /// 可设为 "127.0.0.1" 仅本地回环（配合 Nginx 反向代理时）。
    /// </summary>
    public string ListenAddress { get; set; } = "0.0.0.0";

    /// <summary>
    /// Kestrel 监听的端口号。默认 5000。
    /// 无论最终使用 HTTP 还是 HTTPS，都在此端口上提供服务。
    /// </summary>
    public int ListenPort { get; set; } = 5000;

    /// <summary>
    /// 证书文件夹路径。支持相对路径（相对于 ContentRootPath）和绝对路径。
    /// 默认值 "cert"，即程序同级目录下的 cert/ 文件夹。
    /// </summary>
    public string CertDirectoryPath { get; set; } = "cert";

    /// <summary>
    /// HTTPS 公钥证书文件名。默认 "server.cert"。
    /// </summary>
    public string CertFileName { get; set; } = "server.cert";

    /// <summary>
    /// HTTPS 私钥文件名。默认 "server.key"。
    /// </summary>
    public string CertKeyFileName { get; set; } = "server.key";
}
```

> **设计说明**：
> - 命名遵循 [`backend-development-spec.md` §2.4](../develop/backend-development-spec.md#24-命名约定)：PascalCase，`{Config}Settings` 模式
> - 模式完全参照 [`SpaSettings.cs`](../../src/HysteriaAuth.Master/Config/SpaSettings.cs)（强类型配置类 + `SectionName` 常量）
> - `ListenAddress` + `ListenPort` 解决当前监听配置分散在 `launchSettings.json` / 环境变量 / 默认行为中的模糊问题
> - 证书文件名可配置，适应不同部署场景（如 `fullchain.pem` / `privkey.pem`）

#### 3.2 appsettings.json 新增 Https 配置节

- [ ] **8.2.1** 修改 [`appsettings.json`](../../src/HysteriaAuth.Master/appsettings.json)，在现有配置节后新增（建议放在 `Spa` 配置节之后、`Backup` 之前）：

```json
{
    "Https": {
        "ListenAddress": "0.0.0.0",
        "ListenPort": 5000,
        "CertDirectoryPath": "cert",
        "CertFileName": "server.cert",
        "CertKeyFileName": "server.key"
    }
}
```

**完整的 `appsettings.json` 变更位置**（参照现有文件结构）：

```json
{
    "ConnectionStrings": { ... },
    "MasterServerUrl": "https://master.example.com",
    "Encryption": { ... },
    "Jwt": { ... },
    "Auth": { ... },
    "Node": { ... },
    "Traffic": { ... },
    "Admin": { ... },
    "RateLimit": { ... },
    "Cors": { ... },
    "Spa": {
        "Enabled": true,
        "StaticFilesPath": "wwwroot",
        "FallbackFile": "index.html",
        "CacheMaxAgeSeconds": 86400
    },
    "Https": {                                    // ← 新增
        "ListenAddress": "0.0.0.0",
        "ListenPort": 5000,
        "CertDirectoryPath": "cert",
        "CertFileName": "server.cert",
        "CertKeyFileName": "server.key"
    },
    "Backup": { ... },
    "Logging": { ... }
}
```

> **字段语义说明**：
> 
> | 字段 | 默认值 | 说明 |
> |------|--------|------|
> | `ListenAddress` | `"0.0.0.0"` | Kestrel 监听地址。`0.0.0.0` = 所有网卡（含 IPv4+IPv6）；`127.0.0.1` = 仅本地回环（配合 Nginx 反向代理） |
> | `ListenPort` | `5000` | Kestrel 监听端口。无论 HTTP 还是 HTTPS，统一使用此端口 |
> | `CertDirectoryPath` | `"cert"` | 相对路径，启动时解析为 `{ContentRootPath}/cert/`；支持绝对路径 |
> | `CertFileName` | `"server.cert"` | 公钥证书文件名 |
> | `CertKeyFileName` | `"server.key"` | 私钥文件名 |

#### 3.3 Program.cs 中监听端口规范化 + HTTPS 自动检测逻辑

- [ ] **8.3.1** 修改 [`Program.cs`](../../src/HysteriaAuth.Master/Program.cs)，在服务注册区域新增配置绑定（与其他配置绑定并列）：

```csharp
// 在 Program.cs 的配置强类型绑定区域（约第 17~19 行附近）追加：
builder.Services.Configure<HttpsSettings>(
    builder.Configuration.GetSection(HttpsSettings.SectionName));
```

- [ ] **8.3.2** 实现单端口策略：证书齐全 → HTTPS on `ListenPort`；证书缺失 → HTTP on `ListenPort`。

**完整实现代码**（拆分两段，分别位于 `builder.Build()` 前后）：

```csharp
// ============================================================
// 监听端口规范化 + HTTPS 证书自动检测（Phase 8）
// ============================================================
// 第一段：在 builder.Build() 之前 —— 读取配置 + 路径解析 + 证书检查 + Kestrel 配置

var httpsSettings = builder.Configuration
    .GetSection(HttpsSettings.SectionName)
    .Get<HttpsSettings>() ?? new HttpsSettings();

// 解析证书目录绝对路径（复用 SPA 路径解析规则）
var certAbsolutePath = Path.IsPathRooted(httpsSettings.CertDirectoryPath)
    ? httpsSettings.CertDirectoryPath
    : Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath,
        httpsSettings.CertDirectoryPath));

var certFilePath = Path.Combine(certAbsolutePath, httpsSettings.CertFileName);
var keyFilePath = Path.Combine(certAbsolutePath, httpsSettings.CertKeyFileName);
bool httpsEnabled = File.Exists(certFilePath) && File.Exists(keyFilePath);

// 单端口策略：ListenAddress:ListenPort 上根据证书决定 HTTP 还是 HTTPS
builder.WebHost.ConfigureKestrel(options =>
{
    if (httpsEnabled)
    {
        // 证书齐全 → HTTPS
        options.Listen(
            System.Net.IPAddress.Parse(httpsSettings.ListenAddress),
            httpsSettings.ListenPort,
            listenOptions => listenOptions.UseHttps(certFilePath, keyFilePath));
    }
    else
    {
        // 证书缺失 → HTTP
        options.Listen(
            System.Net.IPAddress.Parse(httpsSettings.ListenAddress),
            httpsSettings.ListenPort);
    }
});

var app = builder.Build();

// ============================================================
// 第二段：在 builder.Build() 之后 —— 日志输出
// ============================================================

if (httpsEnabled)
{
    app.Logger.LogInformation(
        "HTTPS enabled on https://{Address}:{Port} — cert: {CertFile}, key: {KeyFile}, dir: {CertDir}",
        httpsSettings.ListenAddress, httpsSettings.ListenPort,
        httpsSettings.CertFileName, httpsSettings.CertKeyFileName, certAbsolutePath);
}
else
{
    var missingParts = new List<string>();
    if (!File.Exists(certFilePath))
        missingParts.Add($"certificate '{httpsSettings.CertFileName}'");
    if (!File.Exists(keyFilePath))
        missingParts.Add($"private key '{httpsSettings.CertKeyFileName}'");

    var warningMsg =
        $"HTTPS certificate NOT found — {string.Join(", ", missingParts)} missing in '{certAbsolutePath}'. " +
        $"Running in HTTP mode on http://{httpsSettings.ListenAddress}:{httpsSettings.ListenPort} — " +
        $"ALL TRAFFIC IS UNENCRYPTED. This is INSECURE for production!";

    app.Logger.LogWarning("{Warning}", warningMsg);

    // 控制台醒目输出（黄色警告框，确保运维人员不会忽略）
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine();
    Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║  ⚠️  SECURITY WARNING: HTTPS certificate not found!          ║");
    Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
    Console.WriteLine($"║  Certificate directory : {certAbsolutePath}");
    Console.WriteLine($"║  Missing               : {string.Join(", ", missingParts)}");
    Console.WriteLine("║                                                            ║");
    Console.WriteLine($"║  Running in HTTP mode on http://{httpsSettings.ListenAddress}:{httpsSettings.ListenPort}");
    Console.WriteLine("║  ALL TRAFFIC IS UNENCRYPTED — NOT SAFE FOR PRODUCTION!     ║");
    Console.WriteLine("║                                                            ║");
    Console.WriteLine("║  To enable HTTPS:                                          ║");
    Console.WriteLine($"║  1. mkdir -p {certAbsolutePath}");
    Console.WriteLine($"║  2. Place certificate  → {certFilePath}");
    Console.WriteLine($"║  3. Place private key  → {keyFilePath}");
    Console.WriteLine("║  4. Restart the server                                     ║");
    Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
    Console.ResetColor();
    Console.WriteLine();
}
```

> ⚠️ **关键约束**：`app.Logger` 在 `builder.Build()` 之后才可用，而 Kestrel 配置必须在 `Build()` 之前完成。因此证书检查结果用布尔变量 `httpsEnabled` 保存，日志输出延迟到 `Build()` 之后。

#### 3.4 管道位置确认

HTTPS 自动检测代码应插入 [`Program.cs`](../../src/HysteriaAuth.Master/Program.cs) 的以下位置：

```
现有代码区域                              Phase 8 插入位置
──────────────────────────────────────────────────────────────
builder.Services.Configure<HttpsSettings>  ← 与其他 Configure 并列（约第 19 行后）
...Kestrel 配置 + 证书检查 + 条件监听     ← 在 builder.Build() 之前（约第 97 行前）
var app = builder.Build();
...HTTPS 日志输出                          ← 在 Build() 之后、种子数据之前（约第 99 行后）
```

**完整的中间件管道最终顺序**（Phase 8 后）：

```
builder.Build()
  → HTTPS 日志输出 (新增)
  → 数据库自动迁移 + 种子数据
  → UseGlobalExceptionHandler
  → UseCors("AdminCors")
  → UseNodeAuth
  → UseJwtAuth
  → UseAuditContext
  → UseStaticFiles (SPA)
  → MapControllers
  → MapFallbackToFile (SPA)
  → app.Run()
```

#### 3.5 清理 launchSettings.json 中的硬编码端口

- [ ] **8.3.3** 修改 [`Properties/launchSettings.json`](../../src/HysteriaAuth.Master/Properties/launchSettings.json)，将 `applicationUrl` 改为引用 `appsettings.json` 中的端口，或直接移除硬编码使其完全由 `appsettings.json` 的 `Https.ListenPort` 驱动：

```json
{
  "profiles": {
    "HysteriaAuth.Master": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://0.0.0.0:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

> **说明**：`launchSettings.json` 中的 `applicationUrl` 仅影响 `dotnet run` 开发模式。生产环境以 `appsettings.json` 中的 `Https.ListenAddress` + `Https.ListenPort` 为准（Kestrel `ConfigureKestrel` 显式绑定后覆盖所有默认值）。

---

## 4. 应遵守的规范

| 规范来源 | 条款 | Phase 8 适用要点 |
|----------|------|-----------------|
| [SPA 集成设计](../architect/spa-integration.md) | §2.3 | **路径解析规则**：相对路径基于 `ContentRootPath`，绝对路径直接使用，`Path.GetFullPath()` 标准化。`Https.CertDirectoryPath` 的解析逻辑与此完全一致 |
| [安全设计](../architect/security-design.md) | §1 | "HTTPS — 生产环境强制使用 HTTPS"。Phase 8 实现自动检测，使 HTTPS 从"强制"变为"自动启用（当证书就绪时）" |
| [通信安全](../develop/backend-development-spec.md#62-通信安全) | §6.2 | HTTPS 生产环境强制执行。Phase 8 后：证书存在则自动 HTTPS；证书缺失则 HTTP + 醒目 Warning |
| [日志规范](../develop/backend-development-spec.md#10-日志与监控规范) | §10.1 | 日志级别：HTTPS 启用 → `Information`；证书缺失 → `Warning`；控制台额外输出黄色警告框 |
| [配置规范](../develop/backend-development-spec.md#12-配置规范) | §12.1 | 新增 `Https` 配置节，与 `Spa` 配置节同级，都在 `appsettings.json` 中。监听地址/端口从此单一来源读取 |
| [项目结构规范](../architect/project-structure-configuration.md) | §1 | 新增 `Config/HttpsSettings.cs` 置于 `Config/` 目录 |
| [部署规范](../architect/deployment.md) | §1.2 | 部署脚本需新增 `cert/` 目录检查提示（见 §6.4）。systemd `Environment=ASPNETCORE_URLS` 可移除（由 appsettings.json 驱动） |

---

## 5. 应特别注意的事项

### 5.1 单端口策略 — 同一端口、不同协议

这是 Phase 8 最核心的设计决策：

| 场景 | 端口 5000 行为 | 说明 |
|------|---------------|------|
| 证书齐全 | **HTTPS** | `https://your-server:5000/` |
| 证书缺失 | **HTTP** | `http://your-server:5000/` |

- **不需要**额外的 HTTPS 专用端口（如 5443）
- 运维人员始终访问同一个端口，部署证书后自动升级为加密连接
- Nginx 反向代理配置也不需要区分 HTTP/HTTPS 后端端口

### 5.2 日志输出时机

- Kestrel 配置必须在 [`builder.Build()`](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel) 之前完成
- `app.Logger` 必须在 `builder.Build()` 之后才可用
- **因此**：证书检查逻辑需要在 `Build()` 前执行（将结果保存为布尔变量），日志输出在 `Build()` 后执行

### 5.3 路径解析与 SPA 模式一致性

- `Https.CertDirectoryPath` 的解析逻辑**必须**与 `Spa.StaticFilesPath` 完全一致：
  - [`Path.IsPathRooted()`](https://learn.microsoft.com/en-us/dotnet/api/system.io.path.ispathrooted) 判断是否绝对路径
  - 相对路径 → [`Path.GetFullPath(Path.Combine(ContentRootPath, rawPath))`](https://learn.microsoft.com/en-us/dotnet/api/system.io.path.getfullpath)
  - 绝对路径 → 直接使用
- 不得为 HTTPS 证书路径发明新的解析规则

### 5.4 仅检测、不阻止启动

- 证书缺失时 **不阻止启动**，仅输出 Warning 日志 + 控制台警告
- 这与 SPA 的 `wwwroot/` 缺失时的处理策略完全一致（Warning + 继续运行）
- 降级行为：证书缺失 → HTTP only，API 功能完全正常

### 5.5 私钥文件安全

- `server.key` 文件权限应为 `600`（仅 owner 可读写）
- Phase 8 的实现代码不读取密钥内容（仅检查文件是否存在），密钥内容由 Kestrel `UseHttps()` 直接加载
- **禁止**在日志中输出证书/密钥文件的完整内容
- 日志中仅输出文件路径和文件名，不输出文件内容

### 5.6 控制台输出影响

- `Console.ForegroundColor` 仅影响控制台输出，不影响文件日志
- 当作为 systemd 服务运行时，控制台输出进入 journald，可通过 `journalctl -u hysteria-auth-master` 查看
- 控制台警告框使用 Unicode 边框字符（`╔ ╗ ╚ ╝ ║ ═ ╠ ╣`），在大多数现代终端中正常显示

### 5.7 .gitignore 更新

- `cert/` 目录应加入 [`.gitignore`](../../.gitignore)，防止证书文件意外提交到版本控制：

```gitignore
# HTTPS 证书（禁止入库）
cert/*
!cert/.gitkeep
```

- 同时保留 `cert/.gitkeep` 占位文件，确保空目录可被 Git 跟踪

### 5.8 systemd 环境变量简化

Phase 8 后，[`hysteria-auth-master.service`](../../scripts/hysteria-auth-master.service) 中的 `Environment=ASPNETCORE_URLS=http://127.0.0.1:5000` **可移除**。监听地址和端口已由 `appsettings.json` 中的 `Https.ListenAddress` + `Https.ListenPort` 完全接管，`ConfigureKestrel` 显式绑定后 `ASPNETCORE_URLS` 不再生效。

---

## 6. 需要同步更新的文档

> **这是 Phase 8 的强制性交付要求。** 代码落地完成后，以下文档必须同步更新。

### 6.1 backend-development-spec.md 更新

修改 [`document/develop/backend-development-spec.md`](../develop/backend-development-spec.md)，涉及以下章节：

**§6.2 通信安全**：

| 原内容 | 更新后内容 |
|--------|-----------|
| `HTTPS` — 生产环境强制执行 | `HTTPS` — 启动时自动检测 `Https.CertDirectoryPath` 目录下的 `server.cert` + `server.key`，存在则在 `Https.ListenPort` 上启用 HTTPS；缺失则在同端口回退 HTTP 并输出安全警告。监听地址/端口统一由 `appsettings.json` → `Https` 配置节管理 |

**§12.1 主服务器配置 (appsettings.json)**：

在 `Spa` 配置节后新增 `Https` 配置节：

```json
"Https": {
    "ListenAddress": "0.0.0.0",
    "ListenPort": 5000,
    "CertDirectoryPath": "cert",
    "CertFileName": "server.cert",
    "CertKeyFileName": "server.key"
}
```

并在配置项说明表中新增行：

| 配置节 | 关键项 | 说明 |
|--------|--------|------|
| `Https` | `ListenAddress` / `ListenPort` | Kestrel 监听地址和端口。默认 `0.0.0.0:5000`。无论 HTTP/HTTPS 均使用同一端口 |
| `Https` | `CertDirectoryPath` / `CertFileName` / `CertKeyFileName` | HTTPS 证书自动检测：启动时检查该目录下是否存在指定证书文件，存在则启用 HTTPS。支持相对和绝对路径。详见 Phase 8 |

**§13.2 systemd 服务**：

更新 [`hysteria-auth-master.service`](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx) 配置说明——`Environment=ASPNETCORE_URLS` 可移除，监听配置已由 `appsettings.json` 接管。

### 6.2 security-design.md 更新

修改 [`document/architect/security-design.md`](../architect/security-design.md) §1：

| 原内容 | 更新后内容 |
|--------|-----------|
| `HTTPS` — 生产环境强制使用 HTTPS | `HTTPS` — Master Server 启动时自动检测 `Https.CertDirectoryPath` 目录下的证书文件。存在则在 `Https.ListenPort` 上启用 HTTPS；缺失则在同端口回退 HTTP 并在日志和控制台输出醒目安全警告。监听地址/端口由 `appsettings.json` → `Https` 节统一管理。生产环境**强烈建议**部署有效证书。详见 `document/developStage/09-phase8-https-auto-detection.md` |

### 6.3 project-structure-configuration.md 更新

修改 [`document/architect/project-structure-configuration.md`](../architect/project-structure-configuration.md)：

**§1 项目目录结构**：在 `Config/` 目录下新增一行：

```
│   │   ├── Config/
│   │   │   ├── HttpsSettings.cs       # HTTPS 证书自动检测 + 监听端口配置（Phase 8 新增）
```

**§2 主服务器配置**：在 `Spa` 节后新增 `Https` 配置节，并更新配置项说明表，新增 `ListenAddress`/`ListenPort` 两行的说明。

### 6.4 deployment.md 更新

修改 [`document/architect/deployment.md`](../architect/deployment.md) §1.2 部署脚本（或 §9.1.2），在"复制应用文件"步骤后新增 `cert/` 目录检查提示：

```bash
# 部署脚本中新增的 cert 目录检查
echo "Checking HTTPS certificate..."
if [ -f "/opt/hysteria-auth/master/cert/server.cert" ] && 
   [ -f "/opt/hysteria-auth/master/cert/server.key" ]; then
    echo "  -> HTTPS certificate found. Server will use HTTPS on port 5000."
else
    echo "  -> ⚠️  WARNING: HTTPS certificate not found in cert/ directory."
    echo "     The server will run in HTTP mode on port 5000 (UNENCRYPTED)."
    echo "     To enable HTTPS, place your SSL certificate files:"
    echo "       /opt/hysteria-auth/master/cert/server.cert"
    echo "       /opt/hysteria-auth/master/cert/server.key"
    echo "     Then restart: sudo systemctl restart hysteria-auth-master"
fi
```

同时更新 systemd 服务文件说明——`Environment=ASPNETCORE_URLS` 行可注释或移除。

---

## 7. 关键代码模板与示例

### 7.1 完整实现代码（Program.cs 插入片段）

```csharp
// ============================================================
// 监听端口规范化 + HTTPS 证书自动检测（Phase 8）
// ============================================================
// 第一段：在 builder.Build() 之前 —— 读取配置 + 证书检查 + Kestrel 配置

var httpsSettings = builder.Configuration
    .GetSection(HttpsSettings.SectionName)
    .Get<HttpsSettings>() ?? new HttpsSettings();

// 解析证书目录绝对路径（复用 SPA 路径解析规则）
var certAbsolutePath = Path.IsPathRooted(httpsSettings.CertDirectoryPath)
    ? httpsSettings.CertDirectoryPath
    : Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath,
        httpsSettings.CertDirectoryPath));

var certFilePath = Path.Combine(certAbsolutePath, httpsSettings.CertFileName);
var keyFilePath = Path.Combine(certAbsolutePath, httpsSettings.CertKeyFileName);
bool httpsEnabled = File.Exists(certFilePath) && File.Exists(keyFilePath);

// 单端口策略：ListenAddress:ListenPort 上根据证书决定 HTTP 还是 HTTPS
builder.WebHost.ConfigureKestrel(options =>
{
    var addr = System.Net.IPAddress.Parse(httpsSettings.ListenAddress);
    if (httpsEnabled)
    {
        options.Listen(addr, httpsSettings.ListenPort,
            listenOptions => listenOptions.UseHttps(certFilePath, keyFilePath));
    }
    else
    {
        options.Listen(addr, httpsSettings.ListenPort);
    }
});

var app = builder.Build();

// ============================================================
// 第二段：在 builder.Build() 之后 —— 日志输出
// ============================================================

if (httpsEnabled)
{
    app.Logger.LogInformation(
        "HTTPS enabled on https://{Address}:{Port} — cert: {CertFile}, key: {KeyFile}, dir: {CertDir}",
        httpsSettings.ListenAddress, httpsSettings.ListenPort,
        httpsSettings.CertFileName, httpsSettings.CertKeyFileName, certAbsolutePath);
}
else
{
    var missingParts = new List<string>();
    if (!File.Exists(certFilePath))
        missingParts.Add($"certificate '{httpsSettings.CertFileName}'");
    if (!File.Exists(keyFilePath))
        missingParts.Add($"private key '{httpsSettings.CertKeyFileName}'");

    app.Logger.LogWarning(
        "HTTPS certificate NOT found — {Missing} missing in '{CertDir}'. " +
        "Running in HTTP mode on http://{Address}:{Port} — ALL TRAFFIC IS UNENCRYPTED! " +
        "This is INSECURE for production.",
        string.Join(", ", missingParts), certAbsolutePath,
        httpsSettings.ListenAddress, httpsSettings.ListenPort);

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine();
    Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║  ⚠️  SECURITY WARNING: HTTPS certificate not found!          ║");
    Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
    Console.WriteLine($"║  Certificate directory : {certAbsolutePath}");
    Console.WriteLine($"║  Missing               : {string.Join(", ", missingParts)}");
    Console.WriteLine("║                                                            ║");
    Console.WriteLine($"║  Running in HTTP mode on http://{httpsSettings.ListenAddress}:{httpsSettings.ListenPort}");
    Console.WriteLine("║  ALL TRAFFIC IS UNENCRYPTED — NOT SAFE FOR PRODUCTION!     ║");
    Console.WriteLine("║                                                            ║");
    Console.WriteLine("║  To enable HTTPS:                                          ║");
    Console.WriteLine($"║  1. mkdir -p {certAbsolutePath}");
    Console.WriteLine($"║  2. Place certificate  → {certFilePath}");
    Console.WriteLine($"║  3. Place private key  → {keyFilePath}");
    Console.WriteLine("║  4. Restart the server                                     ║");
    Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
    Console.ResetColor();
    Console.WriteLine();
}
```

### 7.2 配置节 JSON 模板

```json
{
    "Https": {
        "ListenAddress": "0.0.0.0",
        "ListenPort": 5000,
        "CertDirectoryPath": "cert",
        "CertFileName": "server.cert",
        "CertKeyFileName": "server.key"
    }
}
```

### 7.3 两种部署场景的行为对照

| 场景 | `ListenAddress` | `ListenPort` | cert/ 目录 | 最终行为 |
|------|:---:|:---:|:---:|------|
| 开发环境 | `0.0.0.0` | `5000` | 不存在 | `http://0.0.0.0:5000` + Warning |
| 生产环境 (Nginx 反代) | `127.0.0.1` | `5000` | 存在且证书齐全 | `https://127.0.0.1:5000` + Nginx `proxy_pass` |
| 生产环境 (直连) | `0.0.0.0` | `5000` | 存在且证书齐全 | `https://your-server:5000` |

### 7.4 .gitignore 追加内容

```gitignore
# Phase 8: HTTPS 证书文件（禁止入库）
cert/*
!cert/.gitkeep
```

---

## 8. 阶段完成标准

| # | 标准 | 验证方式 |
|---|------|----------|
| C1 | `HttpsSettings.cs` 存在且属性完整 | 文件存在，含 `ListenAddress`、`ListenPort`、`CertDirectoryPath`、`CertFileName`、`CertKeyFileName` |
| C2 | `appsettings.json` 包含 `Https` 配置节（5 个字段齐全） | 检查 JSON |
| C3 | `Program.cs` 中有 `Configure<HttpsSettings>` | 代码审查 |
| C4 | `Program.cs` 中使用 `httpsSettings.ListenAddress` + `httpsSettings.ListenPort` 配置 Kestrel | 代码审查，无硬编码端口 |
| C5 | 有 `cert/` 且含 `server.cert` + `server.key` 时，5000 端口使用 **HTTPS** | `curl -k https://localhost:5000/health` → 返回 JSON |
| C6 | 证书齐全时，HTTP 访问被拒绝或重定向（因为端口上只有 HTTPS） | `curl http://localhost:5000/health` → 失败或 0 字节响应（SSL 握手要求） |
| C7 | 无 `cert/` 目录或证书缺失时，5000 端口使用 **HTTP** | `curl http://localhost:5000/health` → 返回 JSON |
| C8 | 无证书时日志输出 Warning 级别 + 完整信息 | 日志/grep → 包含 "HTTPS certificate NOT found" + 路径信息 |
| C9 | 无证书时控制台输出黄色安全警告框（含 `ListenAddress:ListenPort`） | 启动终端可见醒目的安全警告框 |
| C10 | 仅缺失一个文件时的提示准确 | 仅删除 `server.cert` → Warning 提示缺失 `server.cert` |
| C11 | `server.cert` 存在但 `server.key` 不存在时回退 HTTP | Warning 提示缺失 `server.key` |
| C12 | `launchSettings.json` 端口与 `appsettings.json` 无冲突 | 代码审查 |
| C13 | `backend-development-spec.md` 更新完成 | §6.2、§12.1、§13.2 内容更新 |
| C14 | `security-design.md` 更新完成 | §1 内容更新 |
| C15 | `project-structure-configuration.md` 更新完成 | §1 和 §2 内容更新 |
| C16 | `deployment.md` 更新完成 | 部署脚本增加 cert 检查，systemd 说明更新 |
| C17 | `.gitignore` 包含 `cert/*` + `!cert/.gitkeep` | 检查文件内容 |
| C18 | `dotnet build` 通过 | 退出码 0 |
| C19 | `dotnet test` 全部通过（不受影响） | 退出码 0 |

---

## 9. 阶段交接清单

| # | 交付物 | 接收方 | 说明 |
|---|--------|--------|------|
| H1 | `Config/HttpsSettings.cs` | 后端开发 | HTTPS + 监听端口强类型配置类（5 属性） |
| H2 | 更新后的 `appsettings.json` | 后端开发 / DevOps | 新增 `Https` 配置节（含监听地址/端口 + 证书路径） |
| H3 | 更新后的 `Program.cs` | 后端开发 | 单端口策略：证书检测 → HTTPS/HTTP 条件配置 |
| H4 | 更新后的 `launchSettings.json` | 后端开发 | 端口与 `appsettings.json` 一致 |
| H5 | 更新后的 `.gitignore` | 后端开发 / DevOps | cert 目录不入库 |
| H6 | `cert/.gitkeep` 占位文件 | DevOps | 保持空目录可被 Git 跟踪 |
| H7 | 更新后的 `backend-development-spec.md` | 全体开发 | §6.2, §12.1, §13.2 |
| H8 | 更新后的 `security-design.md` | 架构师 / 安全审计 | §1 |
| H9 | 更新后的 `project-structure-configuration.md` | 全体开发 | §1, §2 |
| H10 | 更新后的 `deployment.md` | DevOps | 部署脚本 cert 检查 + systemd 简化 |

---

## 10. 与现有 Phase 的关系

```
Phase 1~5: 核心功能（不涉及 HTTPS 自动检测）
    ↓
Phase 6: SPA 集成 + 路径解析模式（为 Phase 8 提供了技术范式）
    ↓
Phase 7: 节点表扩展（可与 Phase 8 并行）
    ↓
Phase 8 (当前): 监听端口规范化 + HTTPS 单端口自动检测
    ↓
Phase 5 (可并行): 测试与部署（需验证 HTTPS 自动切换行为）
```

> **Phase 8 依赖**：仅依赖 Phase 6 的配置绑定和路径解析模式，与 Phase 7 无依赖关系，可与 Phase 7 并行开发。

---

## 11. 关联文档

| 文档 | 路径 | 说明 |
|------|------|------|
| SPA 集成设计（路径解析模式来源） | [`document/architect/spa-integration.md`](../architect/spa-integration.md) | §2 配置设计 — 路径解析规则 |
| SPA 集成落地指南（Phase 6） | [`document/developStage/06-phase6-spa-integration-deployment.md`](06-phase6-spa-integration-deployment.md) | SpaSettings 配置类模式 |
| 安全设计 | [`document/architect/security-design.md`](../architect/security-design.md) | §1 认证安全 — HTTPS 要求 |
| 后端开发规范 | [`document/develop/backend-development-spec.md`](../develop/backend-development-spec.md) | §6.2 通信安全, §10.1 日志规范, §12.1 配置规范, §13.2 systemd |
| 项目结构与配置 | [`document/architect/project-structure-configuration.md`](../architect/project-structure-configuration.md) | §1 目录结构, §2 主服务器配置 |
| 部署方案 | [`document/architect/deployment.md`](../architect/deployment.md) | §1.2 部署脚本, systemd 服务 |
