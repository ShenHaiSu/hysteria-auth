# Phase 6: SPA 前端集成与部署现代化

> **阶段**: Phase 6 | **预估工期**: 1 周 | **依赖**: Phase 4（完善功能）或与 Phase 4/5 并行
>
> **来源文档**: [`document/architect/spa-integration.md`](../architect/spa-integration.md) · [`../architect/deployment.md`](../architect/deployment.md) · [`../architect/system-architecture.md`](../architect/system-architecture.md) §4 · [`../architect/project-structure-configuration.md`](../architect/project-structure-configuration.md) · [`../develop/backend-development-spec.md`](../develop/backend-development-spec.md) §13.5~13.6 · [`../quick-start.md`](../quick-start.md)

---

## 1. 阶段目标与范围

### 1.1 总体目标

在已有核心功能基础上，完成两项重大升级：

**A. SPA 前端集成** — 使 Master Server 能够托管 Vue3 + Vite7 Web 管理控制台。通过 ASP.NET Core 内置的 `UseStaticFiles` + `MapFallbackToFile` 中间件实现，支持可配置的静态文件路径（相对/绝对路径自动解析）。

**B. 部署现代化** — 将项目从 .NET 8.0 升级至 .NET 10.0，改为自包含（self-contained）发布模式，新增 Windows 开发环境一键构建脚本和 Windows→Linux 交叉编译打包脚本。

Phase 6 完成后，开发者可在 Windows 上一键构建包含前端 SPA 的完整发布包，并直接部署到 Linux 服务器（无需安装 .NET Runtime）。

### 1.2 范围清单

| 序号 | 交付项 | 说明 |
|------|--------|------|
| 6.1 | `SpaSettings` 强类型配置类 | `Enabled`、`StaticFilesPath`、`FallbackFile`、`CacheMaxAgeSeconds` |
| 6.2 | `appsettings.json` 新增 `Spa` 配置节 | 默认启用，路径默认 `"wwwroot"` |
| 6.3 | SPA 静态文件中间件管道集成 | `UseStaticFiles` + `MapFallbackToFile`，含路径解析与目录校验 |
| 6.4 | SPA 静态资源缓存策略 | 对 `.js`/`.css`/`.woff`/`.woff2`/`.ttf`/`.svg`/`.png`/`.ico` 设置长缓存 |
| 6.5 | `wwwroot/` 目录创建 | 前端构建产物默认存放位置 |
| 6.6 | .NET 10.0 升级 | 所有 `.csproj`、Dockerfile、部署脚本中的版本号更新 |
| 6.7 | 自包含发布改造 | `--self-contained true` + RID 指定，systemd `ExecStart` 改为直接执行二进制 |
| 6.8 | Windows 开发环境一键构建脚本 | `scripts/dev-build.ps1` — 后端 Release + 前端 SPA → `publish/local-dev/` |
| 6.9 | Windows→Linux 交叉编译打包脚本 | `scripts/publish-linux.ps1` — 编译 linux-x64 + 前端 + 打包 `.tar.gz` |
| 6.10 | Nginx 双方案适配 | 方案 A（ASP.NET Core 托管，默认）+ 方案 B（Nginx 直接托管静态文件） |
| 6.11 | Docker 多阶段构建更新 | `Dockerfile.master` 新增前端构建阶段 + .NET 10.0 基础镜像 |
| 6.12 | 相关文档更新 | `README.md`、`quick-start.md`、`backend-development-spec.md` 版本号与链接 |

---

## 2. 阶段启动前置检查

> Phase 6 可与 Phase 4/5 并行执行。SPA 中间件依赖 Phase 1（`Program.cs` 管道就绪），部署脚本依赖 Phase 2（Agent 注册就绪）。

| # | 检查项 | 验证内容 | 通过标准 |
|----|--------|----------|----------|
| P5.1 | `Program.cs` 中间件管道完整 | Phase 1~4 的 `UseExceptionHandler`、`UseNodeAuth`、`UseJwtAuth`、`UseCors` 均已注册 | 管道顺序与架构图一致 |
| P5.2 | `appsettings.json` 可正常加载 | Master Server 启动无配置错误 | 日志无配置相关 Error |
| P5.3 | .NET SDK 10.0 已安装 | `dotnet --version` | ≥ 10.0.x |
| P5.4 | Node.js 22+ 已安装（前端构建需要） | `node --version` | ≥ 22.x |
| P5.5 | PowerShell 5.1+ 可用（Windows 构建脚本） | `$PSVersionTable.PSVersion` | ≥ 5.1 |
| P5.6 | 现有 `scripts/deploy-master.sh` 可用 | 检查脚本存在且内容与当前架构一致 | 脚本存在 |
| P5.7 | 现有 `Dockerfile.master` 可用 | 检查 Dockerfile 存在 | 文件存在 |
| P5.8 | ✅ 阅读 [`spa-integration.md`](../architect/spa-integration.md) 全部内容 | — | — |
| P5.9 | ✅ 阅读 [`deployment.md`](../architect/deployment.md) 全部更新内容 | — | — |
| P5.10 | ✅ 阅读 [`backend-development-spec.md`](../develop/backend-development-spec.md) §13.5~13.6 | — | — |

---

## 3. 具体任务清单

### 第 1~2 天：SPA 静态文件托管 + .NET 10.0 升级

#### 3.1 SpaSettings 配置类

- [ ] **6.1.1** 创建 [`Config/SpaSettings.cs`](src/HysteriaAuth.Master/Config/SpaSettings.cs)：

```csharp
namespace HysteriaAuth.Master.Config;

public class SpaSettings
{
    public const string SectionName = "Spa";

    public bool Enabled { get; set; } = true;
    public string StaticFilesPath { get; set; } = "wwwroot";
    public string FallbackFile { get; set; } = "index.html";
    public int CacheMaxAgeSeconds { get; set; } = 86400;
}
```

- [ ] **6.1.2** 在 [`Program.cs`](src/HysteriaAuth.Master/Program.cs) 中绑定配置：
  ```csharp
  builder.Services.Configure<SpaSettings>(
      builder.Configuration.GetSection(SpaSettings.SectionName));
  ```

#### 3.2 appsettings.json 新增 Spa 配置节

- [ ] **6.2.1** 在 [`appsettings.json`](src/HysteriaAuth.Master/appsettings.json) 中新增：

```json
{
    "Spa": {
        "Enabled": true,
        "StaticFilesPath": "wwwroot",
        "FallbackFile": "index.html",
        "CacheMaxAgeSeconds": 86400
    }
}
```

- [ ] **6.2.2** 更新 [`backend-development-spec.md`](document/develop/backend-development-spec.md) §12.1 的配置模板，加入 `Spa` 节（参照 [`spa-integration.md` §2.1](../architect/spa-integration.md#21-appsettingsjson-新增-spa-配置节)）

#### 3.3 SPA 静态文件中间件（Program.cs 管道变更）

- [ ] **6.3.1** 在 [`Program.cs`](src/HysteriaAuth.Master/Program.cs) 的中间件管道中，于 **`UseCors` 之后、`MapControllers` 之前** 插入以下代码块：

```csharp
// ============================
// SPA 静态文件托管
// ============================
var spaSettings = app.Services.GetRequiredService<
    Microsoft.Extensions.Options.IOptions<SpaSettings>>().Value;

if (spaSettings.Enabled)
{
    var contentRoot = app.Environment.ContentRootPath;
    var rawPath = spaSettings.StaticFilesPath;
    var absolutePath = Path.IsPathRooted(rawPath)
        ? rawPath
        : Path.GetFullPath(Path.Combine(contentRoot, rawPath));

    if (Directory.Exists(absolutePath))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(absolutePath),
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

        app.MapFallbackToFile(spaSettings.FallbackFile, new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(absolutePath)
        });

        app.Logger.LogInformation(
            "SPA static files enabled: {Path} (resolved from '{RawPath}')",
            absolutePath, rawPath);
    }
    else
    {
        app.Logger.LogWarning(
            "SPA static files path not found: {Path} (resolved from '{RawPath}'). " +
            "Static file serving is disabled.",
            absolutePath, rawPath);
    }
}
```

- [ ] **6.3.2** 验证管道最终顺序为：
  ```
  UseExceptionHandler → UseNodeAuth → UseJwtAuth → UseCors
  → UseStaticFiles (新增) → MapControllers → MapFallbackToFile (新增)
  ```

> ⚠️ **关键约束**：`MapFallbackToFile` 必须在 `MapControllers` **之后**注册，否则会吞掉所有 `/api/v1/*` 请求。详见 [`spa-integration.md` §3.3](../architect/spa-integration.md#33-路由优先级说明)。

- [ ] **6.3.3** 添加 NuGet 引用（若尚未存在）：
  - `Microsoft.Extensions.FileProviders.Physical`（.NET 框架已内置，通常无需额外安装）

#### 3.4 创建 wwwroot 目录

- [ ] **6.4.1** 在 `src/HysteriaAuth.Master/` 下创建 [`wwwroot/`](src/HysteriaAuth.Master/wwwroot/) 目录
- [ ] **6.4.2** 在 `wwwroot/` 下创建占位文件 `.gitkeep`（确保空目录被 Git 跟踪）
- [ ] **6.4.3** 在 [`.gitignore`](.gitignore) 中添加规则：`wwwroot/` 下除 `.gitkeep` 外忽略（前端构建产物不入库）

#### 3.5 .NET 10.0 版本升级

- [ ] **6.5.1** 更新 [`src/HysteriaAuth.Master/HysteriaAuth.Master.csproj`](src/HysteriaAuth.Master/HysteriaAuth.Master.csproj)：
  ```xml
  <TargetFramework>net10.0</TargetFramework>
  ```

- [ ] **6.5.2** 更新 [`src/HysteriaAuth.Agent/HysteriaAuth.Agent.csproj`](src/HysteriaAuth.Agent/HysteriaAuth.Agent.csproj)：
  ```xml
  <TargetFramework>net10.0</TargetFramework>
  ```

- [ ] **6.5.3** 更新 [`tests/HysteriaAuth.Tests/HysteriaAuth.Tests.csproj`](tests/HysteriaAuth.Tests/HysteriaAuth.Tests.csproj)：
  ```xml
  <TargetFramework>net10.0</TargetFramework>
  ```

- [ ] **6.5.4** 更新 NuGet 包版本（所有项目的 PackageReference 版本号升级至与 .NET 10.0 兼容的最新版本）

- [ ] **6.5.5** 更新 [`docker/Dockerfile.master`](docker/Dockerfile.master) 基础镜像：
  ```dockerfile
  FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
  FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
  ```

- [ ] **6.5.6** 更新 [`docker/Dockerfile.agent`](docker/Dockerfile.agent) 基础镜像（同理）

- [ ] **6.5.7** 更新 [`document/develop/backend-development-spec.md`](document/develop/backend-development-spec.md) §2.1 技术栈：`C# (.NET 10.0)`

- [ ] **6.5.8** 更新所有文档中 `.NET 8.0` → `.NET 10.0` 的引用

---

### 第 3~4 天：构建脚本 + 部署脚本改造

#### 3.6 Windows 开发环境一键构建脚本

- [ ] **6.6.1** 创建 [`scripts/dev-build.ps1`](scripts/dev-build.ps1)（完整内容参照 [`spa-integration.md` §4.1](../architect/spa-integration.md#41-新增脚本scriptsdev-buildps1)）：

```powershell
# dev-build.ps1 — Windows 开发环境一键构建
param(
    [string]$FrontendDistPath = "..\hysteria-auth-web\dist",
    [string]$OutputDir = "publish\local-dev",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path "$ScriptDir\.."

# Step 1: Build .NET Backend (Master + Agent)
Write-Host "[1/4] Building .NET Backend..." -ForegroundColor Yellow
Push-Location $ProjectRoot
dotnet publish src/HysteriaAuth.Master -c $Configuration -r win-x64 --self-contained true -o "$OutputDir"
if ($LASTEXITCODE -ne 0) { throw "Master build failed" }
dotnet publish src/HysteriaAuth.Agent -c $Configuration -r win-x64 --self-contained true -o "$OutputDir\agent"
if ($LASTEXITCODE -ne 0) { throw "Agent build failed" }
Write-Host "  -> Backend build OK" -ForegroundColor Green

# Step 2: Copy Frontend Dist (if exists)
Write-Host "[2/4] Copying frontend SPA dist..." -ForegroundColor Yellow
$frontendAbs = Resolve-Path $FrontendDistPath -ErrorAction SilentlyContinue
if ($frontendAbs) {
    $spaTarget = "$OutputDir\wwwroot"
    if (Test-Path $spaTarget) { Remove-Item -Recurse -Force $spaTarget }
    Copy-Item -Recurse $frontendAbs $spaTarget
    Write-Host "  -> Frontend copied: $($frontendAbs) -> $spaTarget" -ForegroundColor Green
}
else {
    Write-Host "  -> Frontend dist not found at '$FrontendDistPath', skipping." -ForegroundColor Yellow
    Write-Host "     Run 'npm run build' in your Vue project first." -ForegroundColor Yellow
}

# Step 3: Ensure appsettings.json for local dev
Write-Host "[3/4] Checking appsettings.json..." -ForegroundColor Yellow
$appSettings = "$OutputDir\appsettings.json"
if (-not (Test-Path $appSettings)) {
    Copy-Item "$ProjectRoot\src\HysteriaAuth.Master\appsettings.json" $appSettings
    Write-Host "  -> Copied default appsettings.json" -ForegroundColor Green
}
else {
    Write-Host "  -> appsettings.json already exists, preserving." -ForegroundColor Green
}

# Step 4: Verify output
Write-Host "[4/4] Verifying output..." -ForegroundColor Yellow
$exe = "$OutputDir\HysteriaAuth.Master.exe"
if (Test-Path $exe) {
    Write-Host "  -> Master EXE: $exe" -ForegroundColor Green
}
else {
    throw "Master EXE not found at $exe"
}

Write-Host ""
Write-Host "Build Complete! Run: cd $OutputDir && .\HysteriaAuth.Master.exe" -ForegroundColor Green
```

- [ ] **6.6.2** 验证：在 PowerShell 中执行 `.\scripts\dev-build.ps1`
  - 确认 `publish/local-dev/HysteriaAuth.Master.exe` 存在
  - 确认 `publish/local-dev/agent/HysteriaAuth.Agent.exe` 存在
  - 若有前端 dist，确认 `publish/local-dev/wwwroot/index.html` 存在

#### 3.7 Windows→Linux 交叉编译打包脚本

- [ ] **6.7.1** 创建 [`scripts/publish-linux.ps1`](scripts/publish-linux.ps1)（完整内容参照 [`spa-integration.md` §5.1](../architect/spa-integration.md#51-windows--linux-发布)）：

```powershell
# publish-linux.ps1 — Windows→Linux-x64 交叉编译
param(
    [string]$FrontendDistPath = "..\hysteria-auth-web\dist",
    [string]$OutputDir = "publish\linux-x64",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path "$ScriptDir\.."

# Step 1: Publish for linux-x64
Write-Host "[1/3] Publishing for linux-x64..." -ForegroundColor Yellow
Push-Location $ProjectRoot
dotnet publish src/HysteriaAuth.Master -c $Configuration -r linux-x64 --self-contained true -o "$OutputDir"
if ($LASTEXITCODE -ne 0) { throw "Master publish failed" }
dotnet publish src/HysteriaAuth.Agent -c $Configuration -r linux-x64 --self-contained true -o "$OutputDir\agent"
if ($LASTEXITCODE -ne 0) { throw "Agent publish failed" }
Write-Host "  -> Linux-x64 publish OK" -ForegroundColor Green

# Step 2: Copy frontend dist
Write-Host "[2/3] Copying frontend SPA dist..." -ForegroundColor Yellow
$frontendAbs = Resolve-Path $FrontendDistPath -ErrorAction SilentlyContinue
if ($frontendAbs) {
    $spaTarget = "$OutputDir\wwwroot"
    if (Test-Path $spaTarget) { Remove-Item -Recurse -Force $spaTarget }
    Copy-Item -Recurse $frontendAbs $spaTarget
    Write-Host "  -> Frontend copied to wwwroot/" -ForegroundColor Green
}
else {
    Write-Host "  -> Frontend dist not found, skipping. SPA will be disabled." -ForegroundColor Yellow
}

# Step 3: Package for deployment
Write-Host "[3/3] Creating deployment package..." -ForegroundColor Yellow
$packageName = "hysteria-auth-linux-x64-$(Get-Date -Format 'yyyyMMdd-HHmmss').tar.gz"
Push-Location "$ProjectRoot\publish"
tar -czf $packageName "linux-x64"
Pop-Location
Write-Host "  -> Package: publish/$packageName" -ForegroundColor Green

Write-Host ""
Write-Host "Deploy to Linux:" -ForegroundColor White
Write-Host "  scp publish/$packageName user@server:/tmp/" -ForegroundColor White
Write-Host "  ssh user@server" -ForegroundColor White
Write-Host "  cd /opt/hysteria-auth && sudo tar -xzf /tmp/$packageName --strip-components=1" -ForegroundColor White
Write-Host "  sudo systemctl restart hysteria-auth-master" -ForegroundColor White
```

- [ ] **6.7.2** 支持的 RID 清单（参照 [`spa-integration.md` §5.2](../architect/spa-integration.md#52-支持的-rid-runtime-identifier)）：
  | 目标平台 | RID | 说明 |
  |----------|-----|------|
  | Windows x64 | `win-x64` | Windows 10+ / Windows Server 2016+ |
  | Linux x64 | `linux-x64` | Ubuntu 20.04+ / Debian 11+ / CentOS 8+ |
  | Linux ARM64 | `linux-arm64` | 树莓派 4+ / ARM 云服务器 |

#### 3.8 Systemd 部署脚本改造（自包含）

- [ ] **6.8.1** 更新 [`scripts/hysteria-auth-master.service`](scripts/hysteria-auth-master.service)：
  ```ini
  # 关键变更：ExecStart 从 dotnet {dll} 改为直接执行二进制
  ExecStart=/opt/hysteria-auth/master/HysteriaAuth.Master
  ```

- [ ] **6.8.2** 更新 [`scripts/hysteria-auth-agent.service`](scripts/hysteria-auth-agent.service)：
  ```ini
  ExecStart=/opt/hysteria-auth/agent/HysteriaAuth.Agent
  ```

- [ ] **6.8.3** 更新 [`scripts/deploy-master.sh`](scripts/deploy-master.sh)：
  - 移除 .NET Runtime 安装步骤（自包含后不需要）
  - 新增 SPA wwwroot 检查（参照 [`deployment.md` §1.2](../architect/deployment.md#12-部署脚本)）

#### 3.9 Nginx 双方案配置

- [ ] **6.9.1** 确认现有 [`scripts/nginx-master.conf`](scripts/nginx-master.conf) 使用**方案 A**（默认，ASP.NET Core 托管静态文件）：
  - 所有请求透传到 Kestrel
  - 无需额外配置

- [ ] **6.9.2** 在 [`scripts/nginx-master.conf`](scripts/nginx-master.conf) 中**以注释形式**提供**方案 B**（Nginx 直接托管）模板（参照 [`spa-integration.md` §6.2](../architect/spa-integration.md#62-方案-bnginx-直接托管静态文件)）：
  ```nginx
  # === 方案 B：Nginx 直接托管 SPA 静态文件（如需启用，请取消下方注释） ===
  # location / {
  #     root /opt/hysteria-auth/master/wwwroot;
  #     try_files $uri $uri/ /index.html;
  #
  #     location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf)$ {
  #         expires 1d;
  #         add_header Cache-Control "public, immutable";
  #     }
  # }
  #
  # 注意：若使用方案 B，需将 appsettings.json 中 Spa.Enabled 设为 false
  ```

---

### 第 5 天：Docker + 文档 + 验证

#### 3.10 Docker 多阶段构建更新

- [ ] **6.10.1** 更新 [`docker/Dockerfile.master`](docker/Dockerfile.master)，新增前端构建阶段（参照 [`spa-integration.md` §7](../architect/spa-integration.md#7-docker-构建变更)）：

```dockerfile
# Stage 1: 构建前端 SPA
FROM node:22-alpine AS spa-build
WORKDIR /spa
COPY hysteria-auth-web/package*.json ./
RUN npm ci
COPY hysteria-auth-web/ ./
RUN npm run build

# Stage 2: 构建后端
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/HysteriaAuth.Master/HysteriaAuth.Master.csproj", "src/HysteriaAuth.Master/"]
RUN dotnet restore "src/HysteriaAuth.Master/HysteriaAuth.Master.csproj"
COPY . .
WORKDIR "/src/src/HysteriaAuth.Master"
RUN dotnet build "HysteriaAuth.Master.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HysteriaAuth.Master.csproj" -c Release -o /app/publish

# Stage 3: 最终镜像
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=spa-build /spa/dist ./wwwroot
RUN mkdir -p /var/lib/hysteria-auth /var/log/hysteria-auth /var/backups/hysteria-auth
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5000
ENTRYPOINT ["dotnet", "HysteriaAuth.Master.dll"]
```

- [ ] **6.10.2** 验证 Docker 构建：`docker build -f docker/Dockerfile.master -t hysteria-auth-master .`

#### 3.11 文档更新

- [ ] **6.11.1** 更新 [`README.md`](README.md)：
  - 版本号更新至 v1.2
  - 在文档索引中新增 `spa-integration.md` 条目
  - 调整角色阅读导航，增加"前端开发"角色

- [ ] **6.11.2** 更新 [`document/quick-start.md`](document/quick-start.md)（参照最新内容）：
  - 拆分生产/开发环境前提条件
  - 新增 §3 Windows 开发环境一键构建
  - 新增 §4 跨平台编译（Windows→Linux）
  - 新增 SPA 相关 FAQ

- [ ] **6.11.3** 更新 [`document/architect/project-structure-configuration.md`](document/architect/project-structure-configuration.md)：
  - 新增 `SpaSettings` 配置节说明
  - 新增 `wwwroot/` 目录
  - 新增构建脚本路径

- [ ] **6.11.4** 更新 [`document/architect/system-architecture.md`](document/architect/system-architecture.md)：
  - 架构图新增 `SPA 静态文件托管` 组件
  - 新增 §4 SPA 静态文件托管章节

- [ ] **6.11.5** 更新 [`document/develop/backend-development-spec.md`](document/develop/backend-development-spec.md)：
  - .NET 8.0 → 10.0 全部更新
  - 新增 §13.5 SPA 静态文件托管规范
  - 新增 §13.6 跨平台交叉编译规范

#### 3.12 最终验证

- [ ] **6.12.1** `dotnet build` 全部项目，确认无编译错误
- [ ] **6.12.2** `dotnet test` 全部测试通过
- [ ] **6.12.3** 在 Windows 上执行 `.\scripts\dev-build.ps1`，确认产物可运行
- [ ] **6.12.4** 启动 Master Server，确认：
  - 有 `wwwroot/` 时：日志显示 "SPA static files enabled"，`http://localhost:5000/` 返回 index.html
  - 无 `wwwroot/` 时：日志显示 Warning，API 端点仍正常工作
- [ ] **6.12.5** 确认 `/api/v1/*` 路由不受 SPA 中间件影响（兜底路由不吞 API 请求）
- [ ] **6.12.6** 确认 `GET /health` 返回正常（不被 SPA 兜底路由拦截）

---

## 4. 应遵守的规范

| 规范来源 | 条款 | Phase 6 适用要点 |
|----------|------|-----------------|
| [SPA 集成设计](../architect/spa-integration.md) | §2.3 | 路径解析规则：相对路径基于 `ContentRootPath`，绝对路径直接使用，`Path.GetFullPath()` 标准化 |
| [SPA 集成设计](../architect/spa-integration.md) | §3.3 | 路由优先级：`MapControllers` > `UseStaticFiles` > `MapFallbackToFile` |
| [部署规范](../develop/backend-development-spec.md#13-部署规范) | §13.1 | 系统要求：.NET 10.0 Runtime（自包含后可不需要） |
| [部署规范](../develop/backend-development-spec.md#13-部署规范) | §13.5 | SPA 托管：目录不存在时 Warning 不阻止启动、`MapFallbackToFile` 在 `MapControllers` 之后 |
| [部署规范](../develop/backend-development-spec.md#13-部署规范) | §13.6 | 交叉编译：`dotnet publish -r {RID} --self-contained true` |
| [通用编码规范](../develop/backend-development-spec.md#2-通用编码规范) | §2.1 | 技术栈：C# (.NET 10.0) |
| [配置规范](../develop/backend-development-spec.md#12-配置规范) | §12.1 | `Spa` 配置节格式 |
| [日志规范](../develop/backend-development-spec.md#10-日志与监控规范) | §10.1 | SPA 路径解析 Information，目录不存在 Warning |
| [安全规范](../develop/backend-development-spec.md#6-安全规范) | §6.2 | SPA 托管不改变 API 认证机制；CORS 仅对 `/api/*` 路由生效 |
| [API 规范](../develop/backend-development-spec.md#3-api-规范) | §3.6 | CORS 配置对静态文件路径无效 |

---

## 5. 应特别注意的事项

### 5.1 SPA 兜底路由不能吞 API 请求

这是 Phase 6 最容易出错的点。`MapFallbackToFile` 必须放在 `MapControllers` **之后**：

```csharp
// ✅ 正确顺序
app.MapControllers();        // API 优先匹配
app.MapFallbackToFile(...);  // 其余请求兜底

// ❌ 错误顺序 — 所有 /api/v1/* 都会被 index.html 吞掉
app.MapFallbackToFile(...);
app.MapControllers();
```

**验证方法**：启动服务器后，分别访问 `http://localhost:5000/health` 和 `http://localhost:5000/`，前者必须返回 JSON（非 HTML），后者返回 index.html。

### 5.2 路径解析的跨平台兼容

`Path.IsPathRooted()`、`Path.Combine()`、`Path.GetFullPath()` 在不同操作系统上的行为不同：

- Windows：`Path.IsPathRooted("D:\\WebUI\\dist")` → `true`
- Linux：`Path.IsPathRooted("/var/www/spa")` → `true`

路径解析代码（在 Program.cs 中）不应对操作系统做硬编码假设。

### 5.3 自包含发布的大小

`--self-contained true` 会将 .NET Runtime 一起打包：

| 模式 | Master 发布大小（约） | 说明 |
|------|---------------------|------|
| FDD (框架依赖) | ~15MB | 需要目标机器安装 .NET Runtime |
| SCD (自包含) | ~70MB | 完全独立，无需任何运行时 |

SCD 模式适合生产部署（免去 Runtime 安装），但发布包较大。交叉编译打包脚本中的 `.tar.gz` 压缩可以有效减小传输大小。

### 5.4 wwwroot 目录不存在的降级

当 `Spa.Enabled = true` 但 `StaticFilesPath` 目录不存在时：

- ✅ **正确行为**：记录 Warning 日志，跳过静态文件挂载，API 功能正常
- ❌ **错误行为**：抛出异常导致启动失败、或静默失败无日志

### 5.5 静态资源缓存策略

缓存仅对**带 hash 的静态资源**生效（如 `index-abc123.js`）。`index.html` 本身不应缓存（通过 `MapFallbackToFile` 返回，每次请求都读取最新文件）：

```csharp
// OnPrepareResponse 中的扩展名白名单确保仅对资源文件设置缓存
if (ext is ".js" or ".css" or ".woff" or ".woff2" or ".ttf" or ".svg" or ".png" or ".ico")
```

### 5.6 .NET 10.0 升级的 NuGet 兼容性

升级 .NET 版本后，部分 NuGet 包可能需要升级：
- `Microsoft.AspNetCore.Authentication.JwtBearer` — 版本号与框架主版本对齐
- `Microsoft.EntityFrameworkCore.Sqlite` — 同上
- `BCrypt.Net-Next` — 通常无框架版本依赖
- `xunit` / `Moq` / `FluentAssertions` — 检查最新版本兼容性

### 5.7 dev-build.ps1 的前端路径默认值

`dev-build.ps1` 默认前端路径为 `..\hysteria-auth-web\dist`，即假设前端项目与 `server-dev` 平级：

```
hysteria-auth/
├── server-dev/          ← 当前仓库
│   └── scripts/dev-build.ps1
└── hysteria-auth-web/   ← 前端项目（不在本仓库）
    └── dist/
```

如果前端项目在其他位置，通过 `-FrontendDistPath` 参数指定。

### 5.8 Nginx 方案 B 与 SPA 托管冲突

若选择方案 B（Nginx 直接托管），必须将 `appsettings.json` 中的 `Spa.Enabled` 设为 `false`。否则两处同时处理静态文件可能导致不一致的缓存策略。

---

## 6. 关键代码模板与示例

### 6.1 Program.cs 完整管道（变更后）

```csharp
// Program.cs — 中间件管道最终顺序
var app = builder.Build();

// 1. 全局异常处理
app.UseMiddleware<ExceptionMiddleware>();

// 2. 节点认证（仅 /api/v1/auth/* 和 /api/v1/nodes/*）
app.UseMiddleware<NodeAuthMiddleware>();

// 3. JWT 认证（仅 /api/v1/admin/* 和 /api/v1/users/*）
app.UseMiddleware<JwtMiddleware>();

// 4. CORS（仅管理 API）
app.UseCors("AdminCors");

// === Phase 6 新增 ===
// 5. SPA 静态文件托管（条件启用）
var spaSettings = app.Services.GetRequiredService<
    Microsoft.Extensions.Options.IOptions<SpaSettings>>().Value;

if (spaSettings.Enabled)
{
    var absolutePath = Path.IsPathRooted(spaSettings.StaticFilesPath)
        ? spaSettings.StaticFilesPath
        : Path.GetFullPath(Path.Combine(
            app.Environment.ContentRootPath, spaSettings.StaticFilesPath));

    if (Directory.Exists(absolutePath))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(absolutePath),
            OnPrepareResponse = ctx =>
            {
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
            "SPA static files enabled: {Path}", absolutePath);
    }
    else
    {
        app.Logger.LogWarning(
            "SPA directory not found: {Path}. SPA disabled.", absolutePath);
    }
}

// 6. API 路由
app.MapControllers();

// 7. SPA 兜底路由（必须在 MapControllers 之后）
if (spaSettings.Enabled && Directory.Exists(absolutePath))
{
    app.MapFallbackToFile(spaSettings.FallbackFile, new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(absolutePath)
    });
}

app.Run();
```

### 6.2 SpaSettings 配置绑定

```csharp
// Program.cs — 服务注册部分
builder.Services.Configure<SpaSettings>(
    builder.Configuration.GetSection(SpaSettings.SectionName));

// 或使用 Options 模式：
builder.Services.AddOptions<SpaSettings>()
    .Bind(builder.Configuration.GetSection(SpaSettings.SectionName))
    .ValidateDataAnnotations();
```

### 6.3 .csproj 版本升级

```xml
<!-- src/HysteriaAuth.Master/HysteriaAuth.Master.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <!-- ... PackageReference ... -->
</Project>
```

### 6.4 systemd 服务文件（自包含版本）

```ini
# scripts/hysteria-auth-master.service
[Unit]
Description=Hysteria Auth Master Server
After=network.target

[Service]
Type=notify
User=www-data
Group=www-data
WorkingDirectory=/opt/hysteria-auth/master
ExecStart=/opt/hysteria-auth/master/HysteriaAuth.Master    # 自包含二进制，无需 dotnet 前缀
Restart=always
RestartSec=5
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000

[Install]
WantedBy=multi-user.target
```

```ini
# scripts/hysteria-auth-agent.service
[Unit]
Description=Hysteria Auth Edge Agent
After=network.target hysteria-server.service
Requires=hysteria-server.service

[Service]
Type=notify
User=root
Group=root
WorkingDirectory=/opt/hysteria-auth/agent
ExecStart=/opt/hysteria-auth/agent/HysteriaAuth.Agent        # 自包含二进制
Restart=always
RestartSec=5
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

### 6.5 手动交叉编译命令

```bash
# Windows 上编译 Linux 版本
dotnet publish src/HysteriaAuth.Master -c Release -r linux-x64 --self-contained true -o publish/linux-x64
dotnet publish src/HysteriaAuth.Agent -c Release -r linux-x64 --self-contained true -o publish/linux-x64/agent

# Linux 上编译 Windows 版本（通常不需要）
dotnet publish src/HysteriaAuth.Master -c Release -r win-x64 --self-contained true -o publish/win-x64
```

---

## 7. 阶段完成标准

| 标准 | 验证方式 |
|------|----------|
| `SpaSettings.cs` 配置类存在 | 文件存在，属性与配置节一致 |
| `appsettings.json` 包含 `Spa` 配置节 | 检查 JSON，4 个字段齐全 |
| `Program.cs` 管道包含 SPA 中间件 | 代码审查，`UseStaticFiles` + `MapFallbackToFile` |
| `MapFallbackToFile` 在 `MapControllers` 之后 | 代码审查管道顺序 |
| 有 `wwwroot/` 时 SPA 正常托管 | `curl http://localhost:5000/` 返回 index.html |
| 无 `wwwroot/` 时仅 Warning 不崩溃 | 日志有 Warning 无 Error，API 正常 |
| `/health` 不被 SPA 兜底拦截 | `curl http://localhost:5000/health` 返回 JSON |
| `/api/v1/*` 不被 SPA 兜底拦截 | 正常调用任何 API 端点 |
| `.csproj` TargetFramework 为 `net10.0` | 3 个 csproj 文件全部检查 |
| `Dockerfile.master` 基础镜像为 `10.0` | `${SDK_TAG}` 和 `${ASPNET_TAG}` 均为 10.0 |
| `dev-build.ps1` 可在 Windows 上执行 | 产物路径正确，exe 可启动 |
| `publish-linux.ps1` 可产出 `.tar.gz` | `publish/` 下有 tar.gz 文件，内容完整 |
| systemd 服务使用自包含 `ExecStart` | 无 `dotnet` 前缀，直接指向二进制 |
| `docker build` 成功 | `docker build -f docker/Dockerfile.master .` 退出码 0 |
| 所有文档版本号更新 | grep `.NET 8.0` / `net8.0` 无残留 |
| `dotnet build` 无错误 | 退出码 0 |
| `dotnet test` 全通过 | 退出码 0 |

---

## 8. 下一阶段交接清单

> Phase 6 是项目增强阶段，可与 Phase 4/5 并行，也可在全部核心功能完成后执行。以下为 SPA 前端开发团队的参考信息。

| 交接项 | 说明 | 参考位置 |
|--------|------|----------|
| `Spa.StaticFilesPath` 配置灵活性 | 支持相对路径（`wwwroot`）、绝对路径（`/var/www/spa`）、上级目录（`../frontend/dist`） | [`spa-integration.md` §2.3](../architect/spa-integration.md#23-路径解析规则) |
| SPA 前端开发 Vite 配置 | `base: '/'`、代理 `/api` → `http://localhost:5000`、`createWebHistory()` | [`spa-integration.md` §8](../architect/spa-integration.md#8-前端开发注意事项) |
| 前端 API 请求 baseURL | 生产环境同域部署，使用相对路径 `/api/v1` | [`spa-integration.md` §8.3](../architect/spa-integration.md#83-api-请求-base-url) |
| Nginx 方案 B 切换条件 | 高性能需求时可将静态文件交给 Nginx，需同步关闭 `Spa.Enabled` | [`spa-integration.md` §6.2](../architect/spa-integration.md#62-方案-bnginx-直接托管静态文件) |
| 交叉编译 RID 列表 | `win-x64`、`linux-x64`、`linux-arm64` | [`spa-integration.md` §5.2](../architect/spa-integration.md#52-支持的-rid-runtime-identifier) |
| .NET 10.0 破坏性变更 | 升级前查阅 [官方文档](https://learn.microsoft.com/en-us/dotnet/core/compatibility/10.0) | — |
