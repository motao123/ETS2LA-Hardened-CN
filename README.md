# ETS2LA Hardened CN

这是基于 ETS2LA `3.4.37` 的安全加固与中文开发体验派生项目，保留原有 GPL-3.0 许可证和 `ETS2LA.*` 插件 API。项目用于连接 Euro Truck Simulator 2 / American Truck Simulator 的游戏遥测、地图数据、驾驶辅助插件和覆盖层。

> 本项目不包含 ETS2/ATS 游戏资源、用户数据或来源不明的 DLL。没有安装游戏、SCS SDK 和驾驶插件时，仍可启动宿主 UI，但不会产生自动驾驶控制。

## 主要改进

- 插件下载必须使用 HTTPS，并校验 SHA-256；ZIP 解压拒绝目录穿越、重解析点、超大归档和越界路径。
- 插件安装使用 staging、备份和清单核验，更新失败自动回滚；依赖环会在安装前拒绝。
- 控制输出使用线程安全快照、有限浮点值和正权重融合；无效输入回退到中性输出，并可取消、可等待地关闭。
- 配置路径限制在配置根目录；可通过 `ETS2LA_CONFIG_DIR` 隔离实例。
- Windows JWT 使用 DPAPI 保护，不再写入普通 JSON；非 Windows 仅在当前进程内保存令牌。
- 遥测默认关闭；只有用户开启后才创建远程 OTLP exporter。
- 依赖版本集中管理，使用 `global.json` 和 NuGet lock files 固定构建输入。
- 提供 16 个自动化测试，覆盖控制融合、路径安全、ZIP 安全、插件依赖环、设置路径和凭据序列化。

## 系统要求

- Windows x64：.NET 10 SDK 10.0.400（发布包为 self-contained，不需要另装运行时）。
- Linux：.NET 10 SDK、对应的图形/输入依赖和 `linux-x64` 原生运行环境。
- 实际游戏接入还需要合法安装 ETS2/ATS、启用 SCS SDK，并按插件目录安装带可信 SHA-256 的驾驶插件。
- 构建依赖固定提交的 `TruckLib`，当前快照已包含其源码和许可证。

## 构建

在项目根目录执行：

```powershell
# Windows：还原、构建并运行 P0 测试
.\build.ps1 -Test

# Windows：生成可直接启动的 Windows x64 自包含目录
.\build.ps1 -Publish
```

Linux：

```bash
chmod +x build.sh
./build.sh --test
./build.sh --publish
```

如果系统没有 `dotnet`，可以使用工作区上级目录的 `.tools/dotnet/dotnet.exe`；构建脚本会在 Windows 上自动查找该路径。脚本使用 `--locked-mode`，依赖锁文件发生变化时会明确失败，避免静默漂移。

## 运行

构建后运行：

```powershell
.\publish\win-x64\ETS2LA.exe
```

配置文件默认位于 `%AppData%\ETS2LA-Hardened-CN`。测试隔离实例时可设置：

```powershell
$env:ETS2LA_CONFIG_DIR = "$pwd\runtime-config"
.\publish\win-x64\ETS2LA.exe
```

首次启动没有游戏时，遥测和共享内存连接会等待或记录可控警告；请不要在没有真实游戏和安全插件的情况下启用控制输出。

## 测试

```powershell
.\.tools\dotnet\dotnet.exe test tests\ETS2LA.Hardened.Tests\ETS2LA.Hardened.Tests.csproj -c Release --no-restore
```

当前测试是纯逻辑测试，不声称覆盖真实游戏共享内存、图形驱动、第三方插件或在线 API。发布前仍应在隔离测试账号和测试游戏存档上做人工验证。

## 插件安全要求

网络插件元数据必须包含 64 位十六进制 `Sha256`。没有摘要的旧插件会被拒绝安装；声明签名但没有配置可信签名者的包也会被拒绝。插件 DLL 在宿主进程内运行，因此插件本身应来自可审计、可复现的来源。

## 许可证与来源

本项目遵循 GPL-3.0。第三方依赖与 TruckLib 的许可证见 `THIRD_PARTY_NOTICES.md`。修改基于 ETS2LA 3.4.37，并保留原项目版权和免责声明。安全问题请参阅 `SECURITY.md`。
