<#
.SYNOPSIS
    Release 构建脚本 - 支持 Windows/Linux 交叉编译
.DESCRIPTION
    编译后端 Release 版本 + 构建前端 SPA + 合并到统一输出目录 + 打包
    参数:
      -Runtime: 目标运行时 (win-x64 / linux-x64)
      -Target: 构建目标 (Master / Agent)
      -WebPath: 前端项目路径 (相对于 server-dev)
      -OutputDir: 输出目录
#>
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("win-x64", "linux-x64")]
    [string]$Runtime,

    [Parameter(Mandatory = $true)]
    [ValidateSet("Master", "Agent")]
    [string]$Target,

    [string]$WebPath = "..\web-dev",

    [string]$OutputDir = "release-artifacts"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path "$ScriptDir\.."

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Hysteria Auth - Release Build" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Target        : $Target" -ForegroundColor Gray
Write-Host "  Runtime       : $Runtime" -ForegroundColor Gray
Write-Host "  WebPath       : $WebPath" -ForegroundColor Gray
Write-Host "  OutputDir     : $OutputDir" -ForegroundColor Gray
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 确定项目名称和扩展名
$projectName = "HysteriaAuth.$Target"
$isWindows = $Runtime -like "win-*"
$exeExt = if ($isWindows) { ".exe" } else { "" }
$archiveExt = if ($isWindows) { "zip" } else { "tar.gz" }
$archiveName = "$Target-$Runtime"

# 创建临时构建目录
$buildDir = "$ProjectRoot\$OutputDir\build-$Target-$Runtime"
if (Test-Path $buildDir) { Remove-Item -Recurse -Force $buildDir }
New-Item -ItemType Directory -Force -Path $buildDir | Out-Null

# Step 1: Build .NET Backend
Write-Host "[1/4] Building .NET Backend ($Target for $Runtime)..." -ForegroundColor Yellow
Push-Location $ProjectRoot

$publishArgs = @(
    "-c", "Release",
    "-r", $Runtime,
    "--self-contained", "true",
    "/p:PublishTrimmed=false",
    "/p:PublishSingleFile=true"
)

dotnet publish "src/$projectName" @publishArgs -o "$buildDir\app"
if ($LASTEXITCODE -ne 0) { 
    Pop-Location
    throw "$Target build failed for $Runtime" 
}

Write-Host "  -> Backend build OK" -ForegroundColor Green
Pop-Location

# Step 2: Build Frontend (if WebPath is provided and exists)
Write-Host "[2/4] Building Frontend..." -ForegroundColor Yellow
$webAbs = Resolve-Path "$ProjectRoot\$WebPath" -ErrorAction SilentlyContinue
if ($webAbs) {
    Write-Host "  -> Web project found at: $webAbs" -ForegroundColor Gray
    Write-Host "  -> Running 'bun install'..." -ForegroundColor Gray
    Push-Location $webAbs
    
    bun install
    if ($LASTEXITCODE -ne 0) { 
        Pop-Location
        Write-Host "  -> bun install failed, skipping frontend" -ForegroundColor Yellow
    }
    else {
        Write-Host "  -> Running 'bun run build'..." -ForegroundColor Gray
        bun run build
        if ($LASTEXITCODE -ne 0) { 
            Pop-Location
            Write-Host "  -> bun run build failed, skipping frontend" -ForegroundColor Yellow
        }
        else {
            Pop-Location
            $frontendDist = "$webAbs\dist"
            if (Test-Path $frontendDist) {
                $spaTarget = "$buildDir\app\wwwroot"
                if (Test-Path $spaTarget) { Remove-Item -Recurse -Force $spaTarget }
                Copy-Item -Recurse $frontendDist $spaTarget
                Write-Host "  -> Frontend built and copied to wwwroot/" -ForegroundColor Green
            }
            else {
                Write-Host "  -> Frontend dist not found at '$frontendDist'" -ForegroundColor Yellow
            }
        }
    }
}
else {
    Write-Host "  -> Web project not found at '$WebPath', skipping frontend build" -ForegroundColor Yellow
}

# Step 3: Ensure appsettings.json
Write-Host "[3/4] Checking appsettings.json..." -ForegroundColor Yellow
$appSettings = "$buildDir\app\appsettings.json"
if (-not (Test-Path $appSettings)) {
    Copy-Item "$ProjectRoot\src\$projectName\appsettings.json" $appSettings
    Write-Host "  -> Copied default appsettings.json" -ForegroundColor Green
}
else {
    Write-Host "  -> appsettings.json exists, preserving." -ForegroundColor Green
}

# Step 4: Create archive
Write-Host "[4/4] Creating archive..." -ForegroundColor Yellow
$archivePath = "$ProjectRoot\$OutputDir\$archiveName.$archiveExt"

if ($isWindows) {
    # Windows: Create zip using Compress-Archive
    if (Test-Path $archivePath) { Remove-Item -Force $archivePath }
    Compress-Archive -Path "$buildDir\app\*" -DestinationPath $archivePath -Force
}
else {
    # Linux: create tar.gz
    Push-Location "$buildDir"
    if (Test-Path $archivePath) { Remove-Item -Force $archivePath }
    tar -czf $archivePath -C "$buildDir" "app"
    Pop-Location
}

Write-Host "  -> Archive: $archivePath" -ForegroundColor Green

# Cleanup build directory
Remove-Item -Recurse -Force $buildDir

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Release Build Complete!" -ForegroundColor Green
Write-Host " Archive: $OutputDir\$archiveName.$archiveExt" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
