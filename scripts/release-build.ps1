<#
.SYNOPSIS
    Release 构建脚本 - 支持 Windows/Linux 交叉编译
.DESCRIPTION
    编译后端 Release 版本 + 构建前端 SPA + 合并到统一输出目录 + 打包
    参数:
      -Runtime: 目标运行时 (win-x64 / linux-x64)
      -Target: 构建目标 (Master / Agent)
      -WebPath: 前端项目路径 (相对于 server)
      -OutputDir: 输出目录
#>
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("win-x64", "linux-x64")]
    [string]$Runtime,

    [Parameter(Mandatory = $true)]
    [ValidateSet("Master", "Agent")]
    [string]$Target,

    [string]$WebPath = "../web",

    [string]$OutputDir = "release-artifacts"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path (Join-Path $ScriptDir "..")

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
$targetIsWindows = $Runtime -like "win-*"
$archiveExt = if ($targetIsWindows) { "zip" } else { "tar.gz" }
$archiveName = "$Target-$Runtime"

# 创建临时构建目录
$buildDir = Join-Path (Join-Path $ProjectRoot $OutputDir) "build-$Target-$Runtime"
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

dotnet publish "src/$projectName" @publishArgs -o (Join-Path $buildDir "app")
if ($LASTEXITCODE -ne 0) { 
    Pop-Location
    throw "$Target build failed for $Runtime" 
}

Write-Host "  -> Backend build OK" -ForegroundColor Green
Pop-Location

# Step 2: Build Frontend (if WebPath is provided and exists)
Write-Host "[2/4] Building Frontend..." -ForegroundColor Yellow
$webAbs = Resolve-Path (Join-Path $ProjectRoot $WebPath) -ErrorAction SilentlyContinue
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
            $frontendDist = Join-Path $webAbs "dist"
            if (Test-Path $frontendDist) {
                $spaTarget = Join-Path (Join-Path $buildDir "app") "wwwroot"
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

# Step 3: Ensure appsettings.json and create example backup
Write-Host "[3/4] Checking appsettings.json..." -ForegroundColor Yellow
$appDir = Join-Path $buildDir "app"
$appSettings = Join-Path $appDir "appsettings.json"
$exampleAppSettings = Join-Path $appDir "example.appsettings.json"
$sourceAppSettings = Join-Path (Join-Path (Join-Path $ProjectRoot "src") $projectName) "appsettings.json"

# Copy default appsettings.json if not exists
if (-not (Test-Path $appSettings)) {
    Copy-Item $sourceAppSettings $appSettings
    Write-Host "  -> Copied default appsettings.json" -ForegroundColor Green
}
else {
    Write-Host "  -> appsettings.json exists, preserving." -ForegroundColor Green
}

# Always create/update example.appsettings.json as a reference copy
Copy-Item $sourceAppSettings $exampleAppSettings -Force
Write-Host "  -> Created example.appsettings.json (reference copy)" -ForegroundColor Green

# Step 4: Create archive
Write-Host "[4/4] Creating archive..." -ForegroundColor Yellow
$archivePath = Join-Path (Join-Path $ProjectRoot $OutputDir) "$archiveName.$archiveExt"

if ($targetIsWindows) {
    # Windows: Create zip using Compress-Archive
    if (Test-Path $archivePath) { Remove-Item -Force $archivePath }
    Compress-Archive -Path (Join-Path $appDir "*") -DestinationPath $archivePath -Force
}
else {
    # Linux: create tar.gz
    Push-Location $buildDir
    if (Test-Path $archivePath) { Remove-Item -Force $archivePath }
    tar -czf $archivePath -C $buildDir "app"
    Pop-Location
}

Write-Host "  -> Archive: $archivePath" -ForegroundColor Green

# Cleanup build directory
Remove-Item -Recurse -Force $buildDir

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Release Build Complete!" -ForegroundColor Green
Write-Host " Archive: $(Join-Path $OutputDir "$archiveName.$archiveExt")" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
