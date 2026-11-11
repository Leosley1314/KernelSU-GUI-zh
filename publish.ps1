<#
  把本目录的源码 / 文档同步到 GitHub 仓库（成品走 Releases，不进仓库）。
  前置：本机已安装 Git，且已登录过一次 GitHub（凭据由 Git Credential Manager 缓存）。
  用法：.\publish.ps1
#>
param(
    [string]$RepoUrl = "https://github.com/Leosley1314/KernelSU-GUI-zh.git",
    [string]$Branch  = "main",
    [string]$Message = "同步源码与文档"
)

$ErrorActionPreference = "Stop"
$src = Split-Path -Parent $MyInvocation.MyCommand.Path

# Git 可能装了但没加进 PATH，这里兜底找一下
$git = (Get-Command git -ErrorAction SilentlyContinue).Source
if (-not $git) {
    foreach ($c in @("C:\Program Files\Git\cmd\git.exe", "C:\Program Files (x86)\Git\cmd\git.exe", (Join-Path $env:LOCALAPPDATA "Programs\Git\cmd\git.exe"))) {
        if (Test-Path -LiteralPath $c) { $git = $c; break }
    }
}
if (-not $git) { throw "未找到 git，请先安装 Git for Windows：https://git-scm.com/download/win" }

$files = @(
    "README.md", ".gitignore", "build.ps1", "publish.ps1", "release.ps1",
    "ksud_gui.cs", "ksud_gui.manifest",
    "ksud-boot-gui-使用说明.md", "ksud-汉化说明.md"
)

$tmp = Join-Path $env:TEMP ("kernelsu-gui-zh-" + [Guid]::NewGuid().ToString("N").Substring(0, 8))
Write-Host "克隆仓库到 $tmp ..."
& $git clone --branch $Branch $RepoUrl $tmp

if (-not (& $git -C $tmp config user.email)) { & $git -C $tmp config user.email "leosley@users.noreply.github.com" }
if (-not (& $git -C $tmp config user.name))  { & $git -C $tmp config user.name  "Leosley1314" }

foreach ($f in $files) {
    $p = Join-Path $src $f
    if (Test-Path -LiteralPath $p) {
        Copy-Item -LiteralPath $p -Destination (Join-Path $tmp $f) -Force
        Write-Host "  已复制 $f"
    }
    else {
        Write-Warning "缺少 $f，已跳过"
    }
}

& $git -C $tmp add -A
$staged = & $git -C $tmp diff --cached --name-only
if (-not $staged) {
    Write-Host "没有需要提交的变更。"
    Remove-Item -LiteralPath $tmp -Recurse -Force
    return
}

& $git -C $tmp commit -m $Message
& $git -C $tmp push origin $Branch

Remove-Item -LiteralPath $tmp -Recurse -Force
Write-Host "完成：https://github.com/Leosley1314/KernelSU-GUI-zh"
