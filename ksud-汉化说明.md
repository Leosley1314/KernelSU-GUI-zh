# ksud 中文版（KernelSU v3.3.1 · Windows 交叉编译）

## 一、成品

| 项 | 值 |
|---|---|
| 文件 | `ksud-x86_64-pc-windows-gnu-zh.exe` |
| 大小 | 8,025,180 字节（官方原版 8,043,904） |
| SHA256 | `DAB76185A4E9F6D42C058F0C8E99332C3EB97D6AEC93E5FB211778035E3C85F5` |
| 版本 | `ksud 3.3.1`（与官方 `v3.3.1` 标签一致，git describe = v3.3.1） |
| 源码 | github.com/tiann/KernelSU @ tag `v3.3.1`（commit 0c0fdb9601） |
| 内置资产 | 官方 release 的 16 个 LKM（`android12-5.10` … `android17-6.18`，aarch64 + x86_64）与 3 个 `ksuinit`（aarch64/x86_64/riscv64） |

依赖导入表与官方原版**完全一致**（`api-ms-win-core-synch-l1-2-0` / `bcryptprimitives` / `kernel32` / `msvcrt` / `ntdll`），因此运行环境要求与官方版相同（**Windows 8 及以上**）。

## 二、汉化范围

1. **命令与帮助体系**（cli_non_android.rs）—— 程序描述、5 个子命令的描述与全部参数说明。
2. **运行过程输出**（boot_patch.rs、lkm_image.rs）—— 所有步骤提示与失败提示，包括 `boot-patch-v2` 的内核镜像分析诊断（BTF/kallsyms 恢复、ARM64 重定位、ELF 段表、capsule 构造等 200+ 条技术性错误）。
3. **模块参数校验**（boot_patch.rs）—— 模块名与 `block-module`、`FEATURE=VALUE` 格式報错。
4. **特性表描述**（feature_id.rs）。
5. **APK 签名解析**（apk_sign.rs）。
6. **clap 内置框架文案**（本地 vendor 补丁）—— `用法：` `命令：` `选项：` `[默认: …]` `[可选值: …]` 及错误框架。
7. **anyhow 错误串**（本地 vendor 补丁）：`Caused by:` → `由以下原因导致：`
8. **程序错误前缀**（main.rs）：`Error: …` → `错误：…`，并去掉上游重复打印一次的行为。

> 参数名、子命令名、配置键（`allow_shell`、`no_custom_rc`、`block_modules`、`su_compat` …）、正则串、ELF/APK 魔数等**保持不变**，避免脚本与配置文件失效。

## 三、未汉化（保持英文）

| 内容 | 原因 |
|---|---|
| `android-bootimg` 库内部解析错误，如 `invalid boot image`、`unsupported boot version 3` | 第三方 git 依赖（5ec1cff/android_bootimg @150425b），需另建 vendor 才能改。**这是唯一仍会看到的英文**。 |
| Rust 标准库 IO 错误，如 `Negative seek. (os error 131)` | 由标准库生成，无法在不改动 std 的情况下汉化 |
| `RUST_LOG` 打开后的 `log::info!` 调试行 | 面向开发者，保留英文便于对照上游 |

## 四、构建方式（可复现）

环境：Ubuntu 22.04，rustc stable，`x86_64-pc-windows-gnu` std，mingw-w64 GCC 10.3.5，clang 14。

```bash
# 1) 源码
git clone --branch v3.3.1 --single-branch https://github.com/tiann/KernelSU.git ksu-src
cd ksu-src

# 2) 内置资产（官方 release v3.3.1 的 LKM 与 ksuinit，按 CI 目录约定摆放）
#    lkm-aarch64-<kmi>_kernelsu.ko -> userspace/ksud/bin/aarch64/<kmi>_kernelsu.ko
#    ksuinit-aarch64|x86_64|riscv64 -> userspace/ksud/bin/<arch>/ksuinit

# 3) bootstrap 目标文件（必须用 clang 汇编成 aarch64 ELF 对象）
clang --target=aarch64-linux-gnu -c -nostdlib \
  -o userspace/ksud/.lkm_image_bootstrap.o userspace/ksud/src/lkm_image_bootstrap.S

# 4) 交叉编译
export CARGO_TARGET_X86_64_PC_WINDOWS_GNU_LINKER=x86_64-w64-mingw32-gcc
export CC_x86_64_pc_windows_gnu=x86_64-w64-mingw32-gcc
cargo build --release --target x86_64-pc-windows-gnu \
  --manifest-path ./userspace/ksud/Cargo.toml
```

`ksud-zh-hanization.patch` 是对 `userspace/ksud/src` 与根 `Cargo.toml` 的完整 diff（`git apply` 即可复现）。
`Cargo.toml` 里新增的两条 `[patch.crates-io]`（`clap_builder`、`anyhow`）指向本地已打补丁的 vendor 目录。

## 五、使用注意

1. **控制台编码**：程序按 UTF-8 输出；若在旧版 conhost 看到乱码，先执行 `chcp 65001`。
2. **程序名会出现在用法行**：`用法： <文件名> …` 取的是 argv[0]，改名后跟着变，属正常行为。
3. **与官方版可混用**：功能、资产、命令行参数完全相同，只有界面语言不同。
4. **未做真机刷写测试**：已验证 `--help`/`--version`/`supported-kmis`/错误路径，并核对导入表与官方一致；首次刷机前建议先用 `-o` 输出到文件再刷写。
