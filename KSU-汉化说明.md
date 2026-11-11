# KSU 中文版（KernelSU v3.3.1 · Windows 交叉编译）

## 一、成品

| 项 | 值 |
|---|---|
| 文件 | `KSU-x86_64-pc-windows-gnu-zh.exe` |
| 大小 | 8,025,180 字节（官方原版 8,043,904） |
| SHA256 | `DAB76185A4E9F6D42C058F0C8E99332C3EB97D6AEC93E5FB211778035E3C85F5` |
| 版本 | `KSU 3.3.1`（与官方 `v3.3.1` 标签一致，git describe = v3.3.1） |
| 源码 | github.com/tiann/KernelSU @ tag `v3.3.1`（commit 0c0fdb9601） |
| 内置资产 | 官方 release 的 16 个 LKM（`android12-5.10` … `android17-6.18`，aarch64 + x86_64）与 3 个 `ksuinit`（aarch64/x86_64/riscv64） |

依赖导入表与官方原版**完全一致**（`api-ms-win-core-synch-l1-2-0` / `bcryptprimitives` / `kernel32` / `msvcrt` / `ntdll`），因此运行环境要求与官方版相同（**Windows 8 及以上**，`bcryptprimitives.dll` 为系统自带）。

## 二、汉化范围

1. **命令与帮助体系**（cli_non_android.rs）
   - 程序描述、5 个子命令的描述与全部参数说明（`--boot` / `--kernel` / `--module` / `--force-feature` / `--block-module` / `--output` …）
2. **运行过程输出**（boot_patch.rs、lkm_image.rs）
   - 所有 `- 解包 boot 镜像`、`- 注入内核`、`- 重新打包 boot 镜像`、`- 完成！` 等步骤提示
   - 所有失败提示：`- 修补出错：…`、`请指定 boot 镜像`、`不支持 boot 镜像版本 …`、`选项冲突：--ramdisk 与 --kernel` 等
   - `boot-patch-v2` 的内核镜像分析诊断：BTF/kallsyms 恢复、ARM64 重定位、ELF 段表、capsule 构造等 200+ 条技术性错误
3. **模块参数校验**（boot_patch.rs）
   - `模块名不能为空`、`无效：只允许字母、数字和下划线`、`block-module 列表过长…`、`格式应为 FEATURE=VALUE，例 su_compat=0`
4. **特性表描述**（feature_id.rs）：SU 兼容模式 / 内核卸载 / SU 日志 / ADB Root / SELinux 隐藏 / Shell SU 仅限 ADB
5. **APK 签名解析**（apk_sign.rs）：`不是 zip 文件`、`不是已签名的 apk`、`未找到签名块`、`未找到签名！`
6. **clap 内置框架文案**（本地 vendor 补丁）
   - `用法：` `命令：` `选项：` `[默认: …]` `[可选值: …]` `打印帮助` `打印版本`
   - 错误框架：`错误：` `提示：` `以下必需参数未提供：` `发现未知参数 …` `参数 '…' 的取值 '…' 无效` `存在若干相似的子命令：…` `更多信息请尝试 '--help'。`
7. **anyhow 错误串**（本地 vendor 补丁）：`Caused by:` → `由以下原因导致：`
8. **程序错误前缀**（main.rs）：`Error: …` → `错误：…`，并去掉上游重复打印一次的行为（官方原版会把同一条错误打印两遍：一行 `[ERROR KSU::cli_non_android]` + 一行 `Error:`）

> 参数名、子命令名、配置键（`allow_shell`、`no_custom_rc`、`block_modules`、`su_compat` …）、正则串、ELF/APK 魔数等**保持不变**，避免脚本与配置文件失效。

## 三、未汉化（保持英文）

| 内容 | 原因 |
|---|---|
| `android-bootimg` 库内部解析错误，如 `invalid boot image`、`unsupported boot version 3` | 第三方 git 依赖（5ec1cff/android_bootimg @150425b），需另建 vendor 才能改；作为 `错误：` 的下级原因显示行为 `由以下原因导致： invalid boot image`。**这是唯一仍会看到的英文**。 |
| Rust 标准库 IO 错误，如 `Negative seek. (os error 131)` | `std::io::Error` 的 Display 由标准库生成，无法在不改动 std 的情况下汉化 |
| 日志（`RUST_LOG` 打开后的 `log::info!` 调试行） | 面向开发者，保留英文便于对照上游 |

如需要，我可以继续把 `android-bootimg` 也 vendor 进来汉化（约 22 处 `bail!`/`anyhow!`）。

## 四、构建方式（可复现）

环境：Ubuntu 22.04，rustc 1.99.1（rustup stable），`x86_64-pc-windows-gnu` std，mingw-w64 GCC 10.3.5，clang 14（汇编 bootstrap 目标文件）。

```bash
# 1) 源码
git clone --branch v3.3.1 --single-branch https://github.com/tiann/KernelSU.git ksu-src
cd ksu-src

# 2) 内置资产（官方 release v3.3.1 的 LKM 与 ksuinit，按 CI 的目录约定摆放）
#    lkm-aarch64-<kmi>_kernelsu.ko -> userspace/KSU/bin/aarch64/<kmi>_kernelsu.ko
#    lkm-x86_64-<kmi>_kernelsu.ko  -> userspace/KSU/bin/x86_64/<kmi>_kernelsu.ko
#    ksuinit-aarch64|x86_64|riscv64 -> userspace/KSU/bin/<arch>/ksuinit

# 3) bootstrap 目标文件（必须用 clang 汇编成 aarch64 ELF 对象）
clang --target=aarch64-linux-gnu -c -nostdlib \
  -o userspace/KSU/.lkm_image_bootstrap.o userspace/KSU/src/lkm_image_bootstrap.S

# 4) 交叉编译
export CARGO_TARGET_X86_64_PC_WINDOWS_GNU_LINKER=x86_64-w64-mingw32-gcc
export CC_x86_64_pc_windows_gnu=x86_64-w64-mingw32-gcc
cargo build --release --target x86_64-pc-windows-gnu \
  --manifest-path ./userspace/KSU/Cargo.toml
# 产物：target/x86_64-pc-windows-gnu/release/ksud.exe
```

`KSU-zh-hanization.patch` 是对 `userspace/KSU/src` 与根 `Cargo.toml` 的完整 diff（`git apply` 即可复现源码改动）。
`Cargo.toml` 里新增的两条 `[patch.crates-io]`（`clap_builder`、`anyhow`）指向本地已打补丁的 vendor 目录，这两份 vendor 补丁的改动点：

- `clap_builder/src/builder/command.rs`：内置 `--help` / `--version` / `help` 子命令的帮助文案
- `clap_builder/src/output/help_template.rs`：`命令`/`参数`/`选项` 标题、`[默认: ]`、`[可选值: ]`、`[环境变量: ]`
- `clap_builder/src/output/usage.rs`：`用法：`
- `clap_builder/src/error/format.rs`：错误前缀、`以下必需参数未提供`、`发现未知参数`、`取值无效`、`提示：`、`由…相似`、`更多信息请尝试`
- `anyhow/src/fmt.rs`：`Caused by:` → `由以下原因导致：`

## 五、使用注意

1. **控制台编码**：程序按 UTF-8 输出；在 Windows Terminal / PowerShell 7 直接正常显示。若在旧版 conhost 看到乱码，先执行 `chcp 65001`。
2. **程序名会出现在用法行**：`用法： <文件名> …` 取的是 argv[0]，把 exe 改名后用法行会跟着变，属正常行为。
3. **与官方版可混用**：功能、资产、命令行参数完全相同，只有界面语言不同；如遇问题可用官方原版对照判断是否为汉化引入。
4. **未做真机刷写测试**：本次验证覆盖 `--help`/`--version`/`supported-kmis`/错误路径（垃圾 boot 镜像、非法模块名、非法 FEATURE 值），并核对导入表与官方一致；未对真实 boot.img 执行 `boot-patch`，因为没有可用测试镜像。首次刷机前建议先用 `-o` 输出到文件再刷写。
