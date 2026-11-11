<#
  把本目录的源码 / 成品 / 文档一次性推到 GitHub 仓库。
  前置：本机已安装 Git（https://git-scm.com/download/win），首次推送会弹登录。
  用法：在项目目录右键“使用 PowerShell 运行”，或：  .\publish.ps1
#>
param(
    [string]$RepoUrl = "https://github.com/Leosley1314/KernelSU-GUI-zh.git",
    [string]$Branch  = "main"
)

$ErrorActionPreference = "Stop"
$src = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw "未找到 git，请先安装 Git for Windows：https://git-scm.com/download/win"
}

$files = @(
    "README.md", ".gitignore", "build.ps1",
    "ksud_gui.cs", "ksud_gui.manifest",
    "ksud-boot-gui.exe", "ksud-x86_64-pc-windows-gnu-zh.exe",
    "ksud-zh-hanization.patch",
    "ksud-boot-gui-使用说明.md", "ksud-汉化说明.md"
)

$tmp = Join-Path $env:TEMP ("kernelsu-gui-zh-" + [Guid]::NewGuid().ToString("N").Substring(0, 8))
Write-Host "克隆仓库到 $tmp ..."
git clone --branch $Branch $RepoUrl $tmp

if (-not (git -C $tmp config user.email)) { git -C $tmp config user.email "leosley@users.noreply.github.com" }
if (-not (git -C $tmp config user.name))  { git -C $tmp config user.name  "Leosley1314" }

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

git -C $tmp add -A
$staged = git -C $tmp diff --cached --name-only
if (-not $staged) {
    Write-Host "没有需要提交的变更。"
    Remove-Item -LiteralPath $tmp -Recurse -Force
    return
}

git -C $tmp commit -m "同步源码、成品与文档"
git -C $tmp push origin $Branch

Remove-Item -LiteralPath $tmp -Recurse -Force
Write-Host "完成：https://github.com/Leosley1314/KernelSU-GUI-zh"
