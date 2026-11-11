# KernelSU GUI（中文）

KernelSU 命令行工具 `ksud` 的 **Windows 图形化前端**，附带一份 **简体中文汉化** 的 `ksud.exe`。
零第三方依赖：GUI 是单文件 C# WinForms，用系统自带的 `csc.exe` 就能编译；汉化版 KSU 由 KernelSU v3.3.1 源码重建。

> 本仓库只做「调用官方 CLI + 汉化文案 + 图形界面」，不修改 KernelSU 的补丁逻辑，也不涉及设备端内核。

## 下载

成品放在 Releases，仓库里只留源码与文档：

**<https://github.com/Leosley1314/KernelSU-GUI-zh/releases>**

zip 内含 GUI 成品、汉化版 KSU、汉化补丁、源码与文档。解压后把 `KSU-boot-gui.exe` 与 `KSU-x86_64-pc-windows-gnu-zh.exe` 放**同一个文件夹**，双击前者即可。

## 仓库内容

| 文件 | 说明 |
|---|---|
| `KSU_gui.cs` | GUI 源码（C# WinForms 单文件，UTF-8 with BOM） |
| `KSU_gui.manifest` | DPI 感知 / 系统兼容性清单，编译时必需 |
| `build.ps1` | 一键编译（只用系统自带 csc） |
| `publish.ps1` | 把本地源码/文档改动提交并推送到本仓库 |
| `release.ps1` | 打包 zip 并创建/更新 GitHub Release |
| `KSU-boot-gui-使用说明.md` | **功能、三个页签怎么填、FAQ、编译、文件清单** ← 先看这个 |
| `KSU-汉化说明.md` | 汉化范围、可复现的重建步骤 |

功能细节、参数对照、编译命令、更新记录都在 `KSU-boot-gui-使用说明.md`，这里不再重复。

## 快速上手

1. 两个 exe 放同一文件夹。
2. 双击 `KSU-boot-gui.exe` → 选页签 → 选 boot 镜像 → 点「开始修补」。
3. 产物在 `程序目录\输出\`（不存在会自动创建）。

三个页签分别对应 `boot-patch`（经典）、`boot-patch-v2`（新版）、`boot-restore`（还原）。

## 已知限制

KernelSU v3.3.1 要求 boot 镜像 **header version ≥ 3**。`header_version = 2` 的非 GKI 老机型（例如 Redmi K30 Pro 这类 sm8250 + 4.19 内核设备）官方 CLI 无法打补丁，会报 `错误：不支持 boot 镜像版本 2！`。这不是 GUI 或汉化的问题，详见使用说明的 FAQ。

## 许可

- 汉化版 KSU 二进制由 **KernelSU（GPL-3.0）** 源代码修改而来，按 GPL-3.0 分发；上游源码：<https://github.com/tiann/KernelSU>（tag `v3.3.1`）。Releases 附相对该 tag 的完整补丁，可用于还原对应源码。
- GUI 源码 `KSU_gui.cs` 与文档为本项目原创，按 MIT 分发。
