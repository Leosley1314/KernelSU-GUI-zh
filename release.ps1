<#
  一键打包并发布 GitHub Release。
  前置：本机已安装 Git，且已登录过一次 GitHub（凭据由 Git Credential Manager 缓存）。
  用法：
    .\release.ps1 -Tag v1.0.1 -Notes "修了某某问题"
  说明：先把 ksud-boot-gui.exe 编译好（.\build.ps1），本脚本只负责打包 + 上传。
#>
param(
    [Parameter(Mandatory = $true)][string]$Tag,
    [string]$Title = "",
    [string]$Notes = "",
    [string]$Owner = "Leosley1314",
    [string]$RepoName = "KernelSU-GUI-zh"
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
if (-not $git) { throw "未找到 git，无法读取已缓存的 GitHub 凭据。" }

# 1) 取本机缓存的 GitHub 令牌（不会被打印）
$env:GIT_TERMINAL_PROMPT = "0"
$cred = "protocol=https`nhost=github.com`n`n" | & $git credential fill 2>$null
$tok = (($cred -split "`n") | Where-Object { $_ -like "password=*" }) -replace "password=", ""
if (-not $tok) { throw "本机没有可用的 GitHub 凭据，请先执行一次 git push 并登录。" }
$tok = $tok.Trim()

$h = @{
    Authorization = "Bearer $tok"
    Accept        = "application/vnd.github+json"
    "User-Agent"  = "kernelsu-gui-release-script"
}
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# 2) 打包
$stage = Join-Path $env:TEMP ("ksgz-pkg-" + [Guid]::NewGuid().ToString("N").Substring(0, 8))
New-Item -ItemType Directory -Force -Path $stage | Out-Null

$files = @(
    "ksud-boot-gui.exe", "ksud-x86_64-pc-windows-gnu-zh.exe", "ksud-zh-hanization.patch",
    "ksud_gui.cs", "ksud_gui.manifest", "build.ps1",
    "README.md", "ksud-boot-gui-使用说明.md", "ksud-汉化说明.md"
)
foreach ($f in $files) {
    $p = Join-Path $src $f
    if (Test-Path -LiteralPath $p) { Copy-Item -LiteralPath $p -Destination (Join-Path $stage $f) -Force }
    else { Write-Warning "缺少 $f，已跳过" }
}

$assetName = "KernelSU-GUI-zh-$Tag.zip"
$zip = Join-Path $env:TEMP $assetName
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zip -CompressionLevel Optimal
Write-Host ("打包完成：" + $assetName + "  " + [math]::Round((Get-Item -LiteralPath $zip).Length / 1MB, 2) + " MB")

# 3) 查找或创建 Release
$rel = $null
try {
    $rel = Invoke-RestMethod -Uri "https://api.github.com/repos/$Owner/$RepoName/releases/tags/$Tag" -Headers $h -Method Get
    Write-Host "Release $Tag 已存在，将替换其中的同名附件。"
}
catch {
    if ([string]$Title -eq "") { $Title = "KernelSU GUI 中文版 $Tag" }
    $body = @{ tag_name = $Tag; target_commitish = "main"; name = $Title; body = $Notes; draft = $false; prerelease = $false } | ConvertTo-Json
    $rel = Invoke-RestMethod -Uri "https://api.github.com/repos/$Owner/$RepoName/releases" -Headers $h -Method Post -Body $body -ContentType "application/json"
    Write-Host ("已创建 Release：" + $rel.html_url)
}

# 4) 删掉同名旧附件后再上传
foreach ($a in $rel.assets) {
    if ($a.name -eq $assetName) {
        Invoke-RestMethod -Uri ("https://api.github.com/repos/$Owner/$RepoName/releases/assets/" + $a.id) -Headers $h -Method Delete | Out-Null
        Write-Host "已删除旧附件 $assetName"
    }
}

$up = Invoke-RestMethod -Uri ("https://uploads.github.com/repos/$Owner/$RepoName/releases/" + $rel.id + "/assets?name=" + $assetName) `
    -Headers $h -Method Post -InFile $zip -ContentType "application/zip"
Write-Host ("上传完成：" + $up.browser_download_url)

Remove-Item -LiteralPath $stage -Recurse -Force
Remove-Item -LiteralPath $zip -Force
Write-Host ("Release 页面：" + $rel.html_url)
