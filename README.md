# ETS2LA Hardened CN

<div align="center">

## 🚛 把驾驶辅助做成一条可验证的工程链路

**安全插件供应链 · 失效安全控制输出 · 可复现构建 · 中文开发文档**

[快速开始](#快速开始) · [构建发布](#构建发布) · [安全模型](#安全模型)

</div>

---

## 这是什么？

`ETS2LA Hardened CN` 是基于 ETS2LA `3.4.37` 的安全加固派生项目，为 **Euro Truck Simulator 2** 和 **American Truck Simulator** 提供桌面宿主、游戏遥测接入、地图解析、驾驶辅助插件、控制输出和覆盖层能力。

它不是一个“下载后自动接管卡车”的神秘 DLL 包，而是一套可以被还原、编译、测试和审计的完整工程：

- 保留 `ETS2LA.*` 插件 API，方便现有插件生态继续接入；
- 不包含 ETS2/ATS 游戏资源、用户数据或来源不明的二进制；
- 没有游戏、SCS SDK 和驾驶插件时，仍可启动宿主 UI，但不会产生自动驾驶控制；
- 所有高优先级安全改动都有纯逻辑测试，不依赖真实游戏进程才能执行。

## 核心改进

| 能力 | 说明 |
| --- | --- |
| 🔐 插件供应链 | HTTPS、SHA-256、ZIP 路径穿越防护、重解析点拒绝、staging 安装、失败回滚 |
| 🎛️ 控制输出 | 线程安全快照、有限浮点校验、正权重融合、NaN 防护、中性值复位、可取消关闭 |
| 🧩 依赖管理 | 固定 .NET SDK、集中包版本、NuGet lock files、TruckLib 源码随仓库交付 |
| 🛡️ 配置与凭据 | 配置目录边界校验、独立实例目录、Windows DPAPI、JWT 不写入普通 JSON |
| 📡 隐私默认值 | 遥测默认关闭；只有用户开启后才创建远程 OTLP exporter |
| ✅ 质量门 | 16 个自动化测试、Release 构建、Windows x64 self-contained 发布、发布目录 smoke test |
| 🌐 中英双语 | 主界面、设置、插件页、覆盖层与教程全部支持中英即切即用，默认中文 |
| 📊 功能页面 | 实时游戏数据可视化、性能监控面板、路线图内容页 |
| 📚 文档站 | 无构建依赖的静态 GitHub Pages 文档，支持搜索、复制命令和响应式阅读 |

## 下载

最新版本从 [Releases](https://github.com/motao123/ETS2LA-Hardened-CN/releases) 下载，有两种安装方式：

- **Windows 安装器（推荐，支持自动更新）**：下载 `ETS2LA-win-release-Setup.exe` 或 `ETS2LA-win-release.msi`，安装后可从应用内「设置 → 更新」检查并安装新版本。
- **Windows x64 便携版**：下载 `ETS2LA-Hardened-CN-windows-x64.zip`，解压后运行 `ETS2LA.exe`（self-contained，无需另装 .NET）。便携版不支持应用内自动更新，需手动下载替换。
- 每个 Release 附带 `SHA256SUMS-*.txt`，可校验下载包完整性。

完整的「下载 → 连接游戏 → 装插件 → 开驾驶辅助 → 覆盖层交互」图文教程见文档站：

- 使用教程：https://motao123.github.io/ETS2LA-Hardened-CN/tutorial.html

## 快速开始

### 1. 克隆仓库

当前仓库已经包含构建所需的 TruckLib 源码，普通克隆即可：

```bash
git clone https://github.com/motao123/ETS2LA-Hardened-CN.git
cd ETS2LA-Hardened-CN
```

### 2. 安装构建环境

安装 **.NET 10 SDK 10.0.400 或更高的 10.0.x SDK**：

- Windows：https://dotnet.microsoft.com/download/dotnet/10.0
- Linux：使用发行版包管理器或 Microsoft 官方安装方式

检查：

```bash
dotnet --version
```

应输出 `10.0.x`。发布包是 self-contained，终端用户运行发布目录时不需要另装 .NET Runtime；从源码构建仍需要 SDK。

### 3. 构建并测试

Windows PowerShell：

```powershell
.\build.ps1 -Test
```

Linux shell：

```bash
chmod +x build.sh
./build.sh --test
```

脚本会检查 TruckLib、`Assets` 和 `libdeflate`，按目标平台更新锁文件，然后执行 Release 构建和测试。第一次还原需要网络访问 NuGet。

## 构建发布

### Windows x64

```powershell
.\build.ps1 -Publish
```

产物：

```text
publish/win-x64/ETS2LA.exe
```

### Linux x64

```bash
./build.sh --publish
```

产物：

```text
publish/linux-x64/ETS2LA
```

发布脚本会自动执行 smoke test：

```text
Smoke test passed for ETS2LA Hardened CN 0.1.0.0
```

`publish/`、`bin/`、`obj/` 和 `Releases/` 已加入 `.gitignore`，不会被提交。

## 运行与真实游戏接入

直接启动 Windows 发布包：

```powershell
.\publish\win-x64\ETS2LA.exe
```

默认配置目录：

```text
%AppData%\ETS2LA-Hardened-CN
```

测试隔离实例：

```powershell
$env:ETS2LA_CONFIG_DIR = "$pwd\runtime-config"
.\publish\win-x64\ETS2LA.exe
```

要使用真实驾驶辅助，需要额外准备：

1. 合法安装 ETS2 或 ATS；
2. 启用并正确安装对应 SCS SDK；
3. 安装经过审计、带 SHA-256 摘要的驾驶辅助插件；
4. 在测试存档中验证控制行为，再进入日常使用环境。

本仓库不分发游戏资源、用户存档、用户配置、JWT、第三方驾驶插件或来源不明的“优化 DLL”。

## 安全模型

插件 DLL 在宿主进程内运行，因此完整性校验是安装边界，不是进程沙箱。插件元数据必须提供 64 位十六进制 `Sha256`，没有摘要的旧包会被拒绝安装。

安装流程：

```text
HTTPS 下载
   ↓
流式 SHA-256 校验
   ↓
安全 ZIP 解压（拒绝穿越、重解析点、超大归档）
   ↓
staging 目录确认 DLL
   ↓
旧版本备份 + 原子切换
   ↓
清单核验
   ↓
成功，或回滚到旧版本
```

完整说明见 [`SECURITY.md`](SECURITY.md)。第三方组件和许可证见 [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md)。

## 架构速览

```text
SCS 游戏共享内存
        │
        ▼
ETS2LA.Game / Telemetry ──► ETS2LA.State
        │                         │
        │                         ▼
        └──────────────────► 插件算法
                                  │
                                  ▼
             GameOutput / ControlMixer
                                  │
                                  ▼
                        SCS 控制共享内存
```

主要工程：

- `ETS2LA`：宿主入口、生命周期、Velopack 和 OpenTelemetry；
- `ETS2LA.UI`：Avalonia 桌面 UI；
- `ETS2LA.Backend`：插件加载、生命周期和依赖；
- `ETS2LA.Game`：地图、遥测、SDK 和控制输出；
- `ETS2LA.Networking`：插件目录、完整性校验和事务安装；
- `ETS2LA.Overlay` / `ETS2LA.ML`：覆盖层、AR 和视觉能力；
- `TruckLib`：SCS 地图和游戏数据解析；
- `tests/ETS2LA.Hardened.Tests`：安全和控制链纯逻辑测试。

## 测试

```powershell
dotnet test tests\ETS2LA.Hardened.Tests\ETS2LA.Hardened.Tests.csproj -c Release --no-restore
```

或直接使用系统 SDK：

```bash
dotnet test tests/ETS2LA.Hardened.Tests/ETS2LA.Hardened.Tests.csproj -c Release
```

当前测试覆盖：

- 控制权重融合、NaN/Infinity、零权重、油门/刹车语义；
- 插件 ID、路径边界、ZIP 目录穿越和安全解压；
- 插件依赖循环；
- 设置路径穿越；
- JWT 不进入普通 JSON 序列化。

这些测试不代替真实游戏、图形驱动、共享内存、在线 API 和第三方插件的集成测试。

## 贡献与许可

提交行为修改前，请先补充可重复测试；提交安全相关改动时，请同步更新 `SECURITY.md` 和测试用例。不要提交：

- `bin/`、`obj/`、`publish/`、`Releases/`；
- `runtime-config/`、`secrets.dat`、日志、临时文件；
- 游戏资源、用户存档、JWT 或 API 凭据；
- 无法验证来源和哈希的 DLL。

本项目遵循 GPL-3.0，并保留上游版权、免责声明和第三方许可证信息。
