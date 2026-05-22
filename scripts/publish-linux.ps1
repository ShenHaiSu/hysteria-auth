<#
.SYNOPSIS
    Windows → Linux-x64 交叉编译打包脚本
.DESCRIPTION
    在 Windows 开发机上编译出 Linux-x64 自包含可执行程序，合并前端 SPA 构建产物，打包为 .tar.gz
    Phase 6: SPA 集成与部署现代化
#>
param(
    [string]$FrontendDistPath = "..\hysteria-auth-web\dist",
    [string]$OutputDir = "publish\linux-x64",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path "$ScriptDir\.."

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Hysteria Auth - Linux Cross-Compile" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

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
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Cross-Compile Complete!" -ForegroundColor Green
Write-Host " Package: publish/$packageName" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Deploy to Linux:" -ForegroundColor White
Write-Host "  scp publish/$packageName user@server:/tmp/" -ForegroundColor White
Write-Host "  ssh user@server" -ForegroundColor White
Write-Host "  cd /opt/hysteria-auth && sudo tar -xzf /tmp/$packageName --strip-components=1" -ForegroundColor White
Write-Host "  sudo systemctl restart hysteria-auth-master" -ForegroundColor White
