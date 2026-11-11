# KernelSU GUI（中文）

KernelSU `ksud` 命令行工具的 **Windows 图形化前端**，配套一份 **简体中文汉化** 的 `ksud.exe`。
零第三方依赖：GUI 是单文件 C# WinForms，用系统自带的 `csc.exe` 就能编译；汉化版 ksud 由 KernelSU v3.3.1 源码重建。

> 本仓库只做「调用官方 CLI + 汉化文案 + 图形界面」，不修改 KernelSU 的补丁逻辑，也不涉及设备端内核。

## 仓库当前内容

已入库：`README.md`、`.gitignore`、`build.ps1`、`publish.ps1`、`ksud_gui.manifest`、`ksud-boot-gui-使用说明.md`、`ksud-汉化说明.md`。

待补齐（需在本机跑一次 `publish.ps1`，见下节）：`ksud_gui.cs`、`ksud-boot-gui.exe`、`ksud-x86_64-pc-windows-gnu-zh.exe`、`ksud-zh-hanization.patch`。这几个是 76 KB 源码、49 KB / 8 MB 二进制和 110 KB 补丁，超过在线接口能直接内联提交的范围。

## 功能

- 三个页签，只覆盖官方补丁命令：
  - **修补 boot（经典）** → `ksud boot-patch`
  - **修补 boot（新版）** → `ksud boot-patch-v2`
  - **还原 boot** → `ksud boot-restore`
- 字段与 CLI 参数一一对应，工具自带说明文字，不查 `--help` 也能填。
- 高 DPI 友好（按 `GetDpiForWindow` 实测比例缩放，150% 缩放下不错位），窗口固定尺寸不可拉伸。
- 输出位置自动落到 **`程序所在目录\输出`**（不存在自动创建），文件名按页签规则生成 `<镜像名>-ksu.img` / `<镜像名>-restore.img`。
- 支持把 boot 镜像直接拖进窗口；实时流式日志、错误按红字区分（ksud 的 stderr 单独着色）、退出码与耗时状态。
- 配置持久化在 `%APPDATA%\KernelSU-GUI\settings.ini`。

## 文件说明

| 文件 | 说明 | 是否已入库 |
|---|---|---|
| `ksud_gui.cs` | GUI 源码（C# WinForms 单文件，UTF-8 with BOM） | 待补 |
| `ksud_gui.manifest` | DPI 感知 / 系统兼容性清单，编译时必需 | 已入库 |
| `ksud-boot-gui.exe` | GUI 成品（49,152 字节，依赖系统自带 .NET Framework 4.x） | 待补 |
| `ksud-x86_64-pc-windows-gnu-zh.exe` | 简体中文汉化的 `ksud`（8,025,180 字节） | 待补 |
| `ksud-zh-hanization.patch` | 汉化补丁（相对 KernelSU v3.3.1 的完整 diff，2,503 行） | 待补 |
| `ksud-boot-gui-使用说明.md` | 图形界面完整说明 + FAQ + 更新记录 | 已入库 |
| `ksud-汉化说明.md` | 汉化范围、重建步骤、验证方式 | 已入库 |
| `build.ps1` | 一键编译 GUI（只用系统自带 csc） | 已入库 |
| `publish.ps1` | 一键把待补文件做成 commit 推到本仓库 | 已入库 |

## 补齐待入库文件

1. 装一次 Git：<https://git-scm.com/download/win>（只需装一次，首次 push 会弹浏览器登录 GitHub）。
2. 把 4 个待补文件（`ksud_gui.cs`、`ksud-boot-gui.exe`、`ksud-x86_64-pc-windows-gnu-zh.exe`、`ksud-zh-hanization.patch`）放到本 README 同一目录。
3. 在该目录执行：

```powershell
.\publish.ps1
```

脚本会自动 `git clone` 本仓库到临时目录、拷入上表全部文件、提交并推送 `main`，结束会打印仓库地址。

也可以完全手动：

```powershell
git clone https://github.com/Leosley1314/KernelSU-GUI-zh.git
git clone 后拷入文件，然后：
git add -A && git commit -m "同步源码、成品与文档" && git push origin main
```

> 8 MB 的 `ksud-*.exe` 单个文件远低于 GitHub 的 100 MB 限制，直接正常 push 即可，不需要 Git LFS。

## 编译

无需安装 Visual Studio 或 .NET SDK，只用系统自带编译器：

```powershell
.\build.ps1
```

等价于：

```powershell
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /codepage:65001 /optimize+ `
  /win32manifest:ksud_gui.manifest /out:ksud-boot-gui.exe `
  /r:System.Windows.Forms.dll /r:System.Drawing.dll ksud_gui.cs
```

注意：`ksud_gui.cs` 是 **UTF-8 with BOM**，改文件时保留 BOM，否则中文字面量会乱码。

## 使用

1. 把 `ksud-boot-gui.exe` 和 `ksud-x86_64-pc-windows-gnu-zh.exe` 放在**同一个文件夹**。
2. 双击 `ksud-boot-gui.exe`，选页签 → 选 boot 镜像 → 点「开始修补」。
3. 产物默认在 `程序目录\输出\`。

程序会按「程序同目录 → 当前目录 → `D:\ksu`」的顺序自动找 `ksud*.exe`，找不到可以点右上角「更换」手动指定。

## 重要限制：boot 镜像头版本必须 ≥ 3

KernelSU v3.3.1 源码里的 `enforce_bootimage_version`（`userspace/ksud/src/boot_patch.rs`）要求 boot 镜像 **header version ≥ 3**（Android 11+ 的 boot/vendor_boot 分离格式），`boot-patch` 与 `boot-patch-v2` 都会在同一处被拦：

```
错误：不支持 boot 镜像版本 2！
```

header 版本可以在镜像偏移 40 处读 4 字节确认；`ANDROID!` 魔数在偏移 0。老的非 GKI 机型（例如 Redmi K30 Pro 这类 sm8250 + 4.19 内核、`header_version = 2` 的设备）**官方 CLI 走不通**，只能自己编内核把 KernelSU 编进去，或改用支持非 GKI 的第三方分支。这不是 GUI 或汉化的问题。

## 汉化说明

- 汉化对象：KernelSU v3.3.1 官方 Release（`x86_64-pc-windows-gnu`）对应的源码。
- 范围：332 处字面量 + 59 处文档注释，另外 vendor 了 `clap_builder`（`用法:` / `命令:` / `选项:`）与 `anyhow`（`Caused by:` → `由以下原因导致：`）的框架文案；`main.rs` 的错误输出改为 `错误：{e:?}` 并返回退出码 1。
- 重建步骤见 `ksud-汉化说明.md`，补丁见 `ksud-zh-hanization.patch`。

## 许可

- 汉化版 `ksud` 二进制由 **KernelSU（GPL-3.0）** 源码修改而来，按 GPL-3.0 分发；上游源码：<https://github.com/tiann/KernelSU>（tag `v3.3.1`）。本仓库提供了相对该 tag 的完整补丁，可用于还原对应源码。
- GUI 源码 `ksud_gui.cs` 与文档为本项目原创，按 MIT 分发。
