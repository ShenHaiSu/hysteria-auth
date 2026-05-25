#Requires -Version 5.1
<#
.SYNOPSIS
    创建新的发布版本并推送 Git tag
.DESCRIPTION
    1. 执行 git pull 拉取当前分支的所有更新
    2. 获取已存在的 tag，解析为 v{major}.{minor}.{patch} 格式
    3. 通过 CLI 交互询问是否需要手动指定版本号
    4. 如果不需要手动指定，自动将最后一个版本号 +1
    5. 如果手动指定，验证版本号是否已存在，重复则重新输入
    6. 创建新 tag 并推送到远端
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# 颜色输出函数
function Write-Info { param([string]$Message) Write-Host "[INFO] $Message" -ForegroundColor Cyan }
function Write-Success { param([string]$Message) Write-Host "[OK] $Message" -ForegroundColor Green }
function Write-Warn { param([string]$Message) Write-Host "[WARN] $Message" -ForegroundColor Yellow }
function Write-Error { param([string]$Message) Write-Host "[ERROR] $Message" -ForegroundColor Red }

# 检查是否在 git 仓库中
function Test-GitRepository {
    try {
        $result = git rev-parse --is-inside-work-tree 2>$null
        return $result -eq 'true'
    } catch {
        return $false
    }
}

# 获取所有已存在的 tag，过滤出符合 v{major}.{minor}.{patch} 格式的
function Get-ExistingTags {
    $allTags = git tag --list 'v*' 2>$null
    if (-not $allTags) {
        return @()
    }
    
    $validTags = @()
    foreach ($tag in $allTags) {
        $tag = $tag.Trim()
        # 匹配 v{major}.{minor}.{patch} 格式
        if ($tag -match '^v(\d+)\.(\d+)\.(\d+)$') {
            $validTags += @{
                Tag = $tag
                Major = [int]$Matches[1]
                Minor = [int]$Matches[2]
                Patch = [int]$Matches[3]
            }
        }
    }
    
    # 按版本号排序
    return $validTags | Sort-Object { $_.Major }, { $_.Minor }, { $_.Patch }
}

# 获取最新的版本号
function Get-LatestVersion {
    param([array]$Tags)
    
    if ($Tags.Count -eq 0) {
        return @{ Major = 0; Minor = 0; Patch = 0 }
    }
    
    return $Tags[-1]
}

# 生成下一个版本号
function Get-NextVersion {
    param([hashtable]$LatestVersion)
    
    $major = $LatestVersion.Major
    $minor = $LatestVersion.Minor
    $patch = $LatestVersion.Patch + 1
    
    return "v${major}.${minor}.${patch}"
}

# 验证版本号格式
function Test-VersionFormat {
    param([string]$Version)
    
    return $Version -match '^v\d+\.\d+\.\d+$'
}

# 检查版本号是否已存在
function Test-TagExists {
    param([string]$Version)
    
    $existingTags = git tag --list $Version 2>$null
    return $null -ne $existingTags -and $existingTags.Trim() -eq $Version
}

# 获取用户输入的版本号
function Read-VersionFromUser {
    param([array]$ExistingTags)
    
    while ($true) {
        $userInput = Read-Host "请输入版本号 (格式: v{major}.{minor}.{patch}, 例如 v1.0.0)"
        
        # 验证格式
        if (-not (Test-VersionFormat -Version $userInput)) {
            Write-Warn "版本号格式不正确，请使用 v{major}.{minor}.{patch} 格式 (例如 v1.0.0)"
            continue
        }
        
        # 检查是否已存在
        if (Test-TagExists -Version $userInput) {
            Write-Warn "版本号 '$userInput' 已存在，请使用其他版本号"
            continue
        }
        
        return $userInput
    }
}

# 主函数
function Main {
    Write-Info "=== 发布新版本脚本 ==="
    Write-Host ""
    
    # 1. 检查是否在 git 仓库中
    if (-not (Test-GitRepository)) {
        Write-Error "当前目录不是 Git 仓库，请在 Git 仓库根目录下运行此脚本"
        exit 1
    }
    
    # 2. 执行 git pull
    Write-Info "正在拉取最新代码..."
    $pullResult = git pull 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "git pull 失败: $pullResult"
        exit 1
    }
    Write-Success "代码拉取成功"
    Write-Host ""
    
    # 3. 获取已存在的 tag
    Write-Info "正在获取已存在的 tag..."
    $existingTags = Get-ExistingTags
    
    if ($existingTags.Count -gt 0) {
        Write-Info "已找到 $($existingTags.Count) 个版本 tag:"
        foreach ($tag in $existingTags) {
            Write-Host "  $($tag.Tag)"
        }
    } else {
        Write-Info "未找到任何版本 tag"
    }
    Write-Host ""
    
    # 4. 获取最新版本和建议的下一个版本
    $latestVersion = Get-LatestVersion -Tags $existingTags
    $suggestedVersion = Get-NextVersion -LatestVersion $latestVersion
    
    if ($latestVersion.Tag) {
        Write-Info "当前最新版本: $($latestVersion.Tag)"
    } else {
        Write-Info "当前没有版本 tag，将从 v0.0.1 开始"
    }
    Write-Info "建议的下一个版本: $suggestedVersion"
    Write-Host ""
    
    # 5. 询问用户是否需要手动指定版本号
    $manualInput = Read-Host "是否手动指定版本号? (y/N)"
    
    if ($manualInput -match '^[yY](es)?$') {
        # 手动指定版本号
        $newVersion = Read-VersionFromUser -ExistingTags $existingTags
    } else {
        # 使用建议的版本号
        $newVersion = $suggestedVersion
    }
    
    Write-Host ""
    Write-Info "将创建新版本: $newVersion"
    Write-Host ""
    
    # 6. 确认创建
    $confirm = Read-Host "确认创建 $newVersion? (Y/n)"
    if ($confirm -match '^[nN](o)?$') {
        Write-Info "已取消创建"
        exit 0
    }
    
    # 7. 创建 tag
    Write-Info "正在创建 tag: $newVersion..."
    git tag -a $newVersion -m "Release $newVersion" 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "创建 tag 失败"
        exit 1
    }
    Write-Success "tag 创建成功: $newVersion"
    
    # 8. 推送 tag 到远端
    Write-Info "正在推送 tag 到远端..."
    git push origin $newVersion 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "推送 tag 失败"
        exit 1
    }
    Write-Success "tag 推送成功: $newVersion"
    
    Write-Host ""
    Write-Success "=== 版本 $newVersion 发布成功 ==="
}

# 执行主函数
Main
