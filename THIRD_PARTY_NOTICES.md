# 第三方组件说明

本项目继承 ETS2LA 的 GPL-3.0 许可和第三方组件目录。发布时请同时保留各组件随附的许可证文件。

- TruckLib：项目子目录 `TruckLib/`，按其源码随附许可证发布。
- Avalonia、ReactiveUI、Huskui、Optris Icons：UI 依赖，版本见 `Directory.Packages.props`。
- Hexa.NET GLFW/ImGui/ImPlot/OpenGL3：覆盖层依赖。
- SoundFlow 与 FFmpeg codec：音频依赖。
- Velopack：安装与更新依赖。
- OpenTelemetry：观测依赖；默认不启用远程 exporter。
- SharpDX、SDL3、SharpHook：平台输入依赖。
- Newtonsoft.Json、System.Drawing.Common、Microsoft.Extensions.FileSystemGlobbing、Mono.Options：数据和提取器依赖。
- `libdeflate` 和随 SDK 资产提供的控制器组件：请保留其来源文件和各自许可证声明。

准确版本和还原来源以 `Directory.Packages.props`、各项目 `packages.lock.json` 与依赖包内许可证为准。未知来源的二进制不属于本项目发行内容。
