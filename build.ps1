# 一键编译 GUI（只用系统自带 csc，不需要 SDK）
# 用法：右键“使用 PowerShell 运行”，或在终端里 .\build.ps1

$dir = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path -LiteralPath $csc)) {
    throw "找不到 csc.exe，请确认系统已安装 .NET Framework 4.x"
}
if (-not (Test-Path -LiteralPath "$dir\ksud_gui.cs")) {
    throw "缺少 ksud_gui.cs（源码未上传时请先本地放入）"
}

& $csc /nologo /target:winexe /codepage:65001 /optimize+ `
    /win32manifest:"$dir\ksud_gui.manifest" /out:"$dir\ksud-boot-gui.exe" `
    /r:System.Windows.Forms.dll /r:System.Drawing.dll "$dir\ksud_gui.cs"

if ($LASTEXITCODE -ne 0) {
    throw "编译失败，退出码 $LASTEXITCODE"
}
Write-Host "已生成 $dir\ksud-boot-gui.exe"
