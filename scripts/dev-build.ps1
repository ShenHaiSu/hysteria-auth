<#
.SYNOPSIS
    Windows 开发环境一键构建脚本
.DESCRIPTION
    编译后端 Release 版本 + 复制前端 SPA 构建产物 + 合并到统一输出目录
    使用 PublishTrimmed + PublishSingleFile 减少输出 DLL 数量
#>
param(
    [string]$FrontendDistPath = "..\hysteria-auth-web\dist",
    [string]$OutputDir = "publish\local-dev",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path "$ScriptDir\.."

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Hysteria Auth - Dev Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Configuration : $Configuration" -ForegroundColor Gray
Write-Host "  Runtime       : win-x64" -ForegroundColor Gray
Write-Host "  PublishMode   : SingleFile + Trimmed" -ForegroundColor Gray
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build .NET Backend (Master + Agent)
Write-Host "[1/4] Building .NET Backend..." -ForegroundColor Yellow
Write-Host "  -> Master: HysteriaAuth.Master" -ForegroundColor Gray
Write-Host "  -> Agent : HysteriaAuth.Agent" -ForegroundColor Gray
Push-Location $ProjectRoot

$publishArgs = @(
    "-c", $Configuration,
    "-r", "win-x64",
    "--self-contained", "true",
    "/p:PublishTrimmed=true",
    "/p:PublishSingleFile=true"
)

dotnet publish src/HysteriaAuth.Master @publishArgs -o "$OutputDir"
if ($LASTEXITCODE -ne 0) { throw "Master build failed" }

dotnet publish src/HysteriaAuth.Agent @publishArgs -o "$OutputDir\agent"
if ($LASTEXITCODE -ne 0) { throw "Agent build failed" }

Write-Host "  -> Backend build OK" -ForegroundColor Green

# Step 2: Copy Frontend Dist (if exists)
Write-Host "[2/4] Copying frontend SPA dist..." -ForegroundColor Yellow
if ([string]::IsNullOrEmpty($FrontendDistPath)) {
    Write-Host "  -> Frontend dist path is empty, skipping." -ForegroundColor Yellow
}
else {
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
    Write-Host "  -> Run: $exe" -ForegroundColor Green
}
else {
    throw "Master EXE not found at $exe"
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Build Complete!" -ForegroundColor Green
Write-Host " Output: $OutputDir" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "To start the master server:" -ForegroundColor White
Write-Host "  cd $OutputDir" -ForegroundColor White
Write-Host "  .\HysteriaAuth.Master.exe" -ForegroundColor White
Write-Host ""
Write-Host "Then open: http://localhost:5000  (SPA frontend)" -ForegroundColor White
Write-Host ""
