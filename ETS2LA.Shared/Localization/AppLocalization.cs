namespace ETS2LA.Shared.Localization;

/// <summary>
/// 轻量级共享本地化核心。主程序 (ETS2LA.UI) 与覆盖层 / 教程 (ETS2LA.Overlay、
/// ETS2LA.Tutorials) 都依赖这一层，因此切换语言后游戏内 ImGui 界面也会同步。
/// 源文本以简体中文为默认，切换到英文时通过本表反向翻译。
/// </summary>
public static class AppLocalization
{
    private static readonly Dictionary<string, string> Literals = new(StringComparer.Ordinal)
    {
        // 覆盖层通用
        ["交互模式"] = "Interaction mode",
        ["点击"] = "Click to ",
        ["隐藏"] = "hide",
        ["显示"] = "show",
        ["此窗口"] = " this window",
        ["性能叠加层"] = "Performance overlay",
        ["叠加层空闲 CPU 时间百分比"] = "Overlay free CPU time percentage",

        // 覆盖层窗口
        ["控制台"] = "Console",
        ["叠加层信息"] = "Overlay info",
        ["演示窗口"] = "Demo window",
        ["视觉摄像头"] = "Vision cameras",
        ["状态信息"] = "State info",
        ["摄像头"] = "Camera",
        ["未绑定"] = "Unbound",

        // OverlayInfo 说明
        ["*惊喜* 这里有一个新窗口 O_O"] = "*Surprise* there's a new window O_O",
        ["这是最终会在游戏上方显示信息的叠加层。对于 C#，我们实际上已经让它比以前强大得多！"] = "This is the overlay that will eventually render information on top of the game. For C#, we've made it a lot more than it was before!",
        ["插件开发者现在可以完全访问 ImGui 进行渲染，希望能看到一些有趣的成果！"] = "Plugin developers now have full access to ImGui for rendering. Hopefully we will see some interesting results!",
        ["目前我们只实现了基础功能，遥测插件将展示在渲染大量数据时不错的性能。"] = "For now we have only implemented the basics; telemetry plugins will show off the performance when rendering lots of data.",
        ["按住以下按键即可与叠加层交互"] = "Hold the key below to interact with the overlay",
        ["（可在设置中修改！）"] = "(can be changed in settings!)",
        ["叠加层基本上是一个完整的窗口系统，理论上不应该发生崩溃……希望如此。如果真的发生了，请报告问题！"] = "The overlay is basically a full window system; it shouldn't crash... hopefully. But if it does, please report it!",

        // 教程通用
        ["下一步"] = "Next",
        ["覆盖层简介"] = "Introduction to the overlay",
        ["用户界面简介"] = "Introduction to the user interface",
        ["教程完成"] = "Tutorial finished",
        ["欢迎使用 ETS2LA！"] = "Welcome to ETS2LA!",
        ["让我们先熟悉一下用户界面。"] = "Let's start by getting familiar with the user interface.",
        ["你现在看到的窗口是覆盖层。"] = "The window you are seeing right now is an overlay.",
        ["按住覆盖层交互键即可继续。"] = "Hold the overlay interaction key to continue.",
        ["很好！"] = "Great!",
        ["这个覆盖层用于 ETS2LA 的许多功能。"] = "This overlay is used for many features in ETS2LA.",
        ["如果你不喜欢当前按键绑定，之后可以在设置中修改。"] = "If you don't like the current keybind, you can change it in settings later.",
        ["现在已进入覆盖层模式，你可以与窗口交互。"] = "You are now in overlay mode and can interact with windows.",
        ["试着拖动这个窗口来移动它！"] = "Try dragging this window to move it!",
        ["太棒了！"] = "Awesome!",
        ["记住，如需与覆盖层窗口交互，请先进入交互模式！"] = "Remember to enter interaction mode first if you need to interact with overlay windows!",
        ["退出覆盖层交互模式即可继续。"] = "Exit overlay interaction mode to continue.",

        // 控制台 / 状态窗口
        ["清空控制台"] = "Clear console",
        ["显示更多日志"] = "Show more logs",
        ["显示更少日志"] = "Show fewer logs",
        ["目标转向等级："] = "Desired steering level:",
        ["暂停转向辅助："] = "Pause steering assist:",
        ["目标纵向等级："] = "Desired longitudinal level:",
        ["暂停纵向辅助："] = "Pause longitudinal assist:",
        ["目标速度："] = "Desired speed:",
        ["显示单位："] = "Display units:",

        // 教程提示 / 消息（含换行）
        ["从插件目录安装完成前的入门教程。"] = "Onboarding tutorial before installing from the plugin catalogue.",
        ["从插件目录安装完成后的入门教程。"] = "Onboarding tutorial after installing from the plugin catalogue.",
        ["这个侧边栏包含你需要的所有功能。\n我们先前往插件目录页面。"] = "This sidebar contains everything you need.\nLet's go to the plugin catalogue page first.",
        ["很好！\n接下来我们去插件管理器页面。"] = "Great!\nNext, let's go to the plugin manager page.",
        ["很好！\n接下来我们去设置。"] = "Great!\nNext, let's go to settings.",
        ["请安装“车道辅助”和“自适应巡航控制”插件。"] = "Please install the \"Lane Assist\" and \"Adaptive Cruise Control\" plugins.",
        ["这里显示了你已安装的所有插件。首先启用“车道辅助”和“自适应巡航控制”插件。"] = "This shows all the plugins you have installed. First, enable the \"Lane Assist\" and \"Adaptive Cruise Control\" plugins.",
        ["我们来看看控制设置。"] = "Let's take a look at the controls settings.",
        ["你可能已经注意到依赖项会自动安装。\n每次安装或卸载插件、库之后，都需要重启 ETS2LA。\n在某些系统上，你可能需要手动重启 ETS2LA。"] = "You may have noticed dependencies are installed automatically.\nEvery time you install or uninstall a plugin or library, ETS2LA must be restarted.\nOn some systems you may need to restart ETS2LA manually.",
        ["你可以在这里查看 ETS2LA 的所有控制。我们还打开了“状态信息”窗口，方便你查看当前设置。\n你也可以使用“ASSIST”在不同转向模式之间切换。"] = "Here you can view all of ETS2LA's controls. We also opened the \"Status info\" window so you can see your current settings.\nYou can also use \"ASSIST\" to switch between steering modes.",
        ["你可以在这里查看 ETS2LA 的所有控制。我们还打开了“状态信息”窗口，方便你查看当前设置。\n请按下“SET”切换自适应巡航控制。"] = "Here you can view all of ETS2LA's controls. We also opened the \"Status info\" window so you can see your current settings.\nPress \"SET\" to toggle adaptive cruise control.",
        ["入门教程到此结束。如有疑问，请查看我们的 YouTube 频道和 Discord 获取更多信息。\n仪表盘页面中可以找到所有链接。"] = "This concludes the onboarding tutorial. If you have questions, check our YouTube channel and Discord for more information.\nAll links can be found on the dashboard page.",
        ["已连接"] = "Connected",
        ["未连接"] = "Not connected",
        ["已暂停"] = "Paused",
        ["运行中"] = "Running",
        ["油门 {0} / 刹车 {1} / 转向 {2}"] = "Throttle {0} / Brake {1} / Steering {2}",
        ["双闪"] = "Hazard",
        ["左转"] = "Left",
        ["右转"] = "Right",
        ["关"] = "Off",
        ["驻车制动"] = "Parking brake",
        ["行车"] = "Driving",
        ["发动机启动"] = "Engine running",
        ["发动机未启动"] = "Engine off",
        ["性能监控"] = "Performance monitor",
        ["渲染帧率 (FPS)"] = "Render frame rate (FPS)",
        ["单帧耗时 (ms)"] = "Frame time (ms)",
        ["剩余渲染时间 (ms)"] = "Remaining render time (ms)",
        ["CPU 使用率"] = "CPU usage",
        ["进程内存 (MB)"] = "Process memory (MB)",
        ["托管堆内存 (MB)"] = "Managed heap memory (MB)",
        ["游戏连接状态"] = "Game connection status",
        ["游戏暂停状态"] = "Game paused status",
        ["说明：帧率来自游戏内覆盖层渲染循环；CPU 使用率按上一采样周期的进程时间计算。"] = "Note: frame rate comes from the in-game overlay render loop; CPU usage is calculated from the process time of the previous sampling period.",
        ["游戏数据可视化"] = "Game data visualization",
        ["连接状态"] = "Connection status",
        ["速度 (km/h)"] = "Speed (km/h)",
        ["发动机转速 (RPM)"] = "Engine RPM",
        ["档位"] = "Gear",
        ["巡航控制速度"] = "Cruise control speed",
        ["油门 / 刹车 / 转向"] = "Throttle / Brake / Steering",
        ["燃油"] = "Fuel",
        ["转向灯"] = "Indicators",
        ["停车制动 / 发动机"] = "Parking brake / Engine",
        ["卡车"] = "Truck",
        ["货物"] = "Cargo",
        ["出发城市"] = "Origin city",
        ["目的地城市"] = "Destination city",
        ["坐标 X"] = "Coordinate X",
        ["坐标 Y"] = "Coordinate Y",
        ["坐标 Z"] = "Coordinate Z",
        ["说明：本页实时读取游戏遥测数据，进入驾驶状态后数值会持续刷新。"] = "Note: this page reads game telemetry in real time; values keep refreshing once you enter driving.",
        ["已完成"] = "Completed",
        ["规划中"] = "Planned",
        ["GitHub 仓库"] = "GitHub repository",
        ["原作者项目"] = "Original project",
        ["• P0 — 插件供应链与下载解压安全加固"] = "• P0 — Plugin supply chain and archive extraction hardening",
        ["• P0 — 控制输出失效安全（权重融合与中性值回退）"] = "• P0 — Failsafe control output (weight mixing and neutral fallback)",
        ["• P1 — 配置路径校验、JWT 凭据保护与原子保存"] = "• P1 — Config path validation, JWT credential protection and atomic save",
        ["• P2 — 完整中英文界面与本地化切换"] = "• P2 — Full bilingual interface and locale switching",
        ["• 游戏遥测连接与性能监控面板"] = "• Game telemetry connection and performance monitoring panel",
        ["• 游戏地图可视化渲染"] = "• Game map visualization rendering",
        ["• GitHub 自动更新与安装包发布"] = "• GitHub auto-update and installer publishing",
        ["• 更多驾驶辅助功能与插件生态"] = "• More driving assistance features and plugin ecosystem",
        ["无"] = "None",
        ["车道保持"] = "Lane keep",
        ["完整"] = "Full",
        ["紧急制动"] = "Emergency braking",
        ["自适应巡航"] = "Adaptive cruise",
        ["是"] = "Yes",
        ["否"] = "No",
        ["已激活"] = "Active",
        ["未激活"] = "Inactive",
        ["转向辅助等级"] = "Steering assist level",
        ["纵向辅助等级"] = "Longitudinal assist level",
        ["转向辅助暂停"] = "Steering assist paused",
        ["纵向辅助暂停"] = "Longitudinal assist paused",
        ["辅助目标速度 (km/h)"] = "Assist target speed (km/h)",
        ["巡航控制"] = "Cruise control",
        ["已加载插件"] = "Loaded plugins",
        ["新鲜"] = "Fresh",
        ["陈旧（已复位输出）"] = "Stale (outputs reset)",
        ["不限速"] = "No limit",
        ["未生效（未设限速）"] = "Inactive (no limit set)",
        ["未超速"] = "Not overspeeding",
        ["未生效"] = "Inactive",
        ["超速 +{0} {1}"] = "Overspeed +{0} {1}",
        ["正在制动"] = "Braking",
        ["禁止油门"] = "Throttle cut",
        ["安全兜底状态"] = "Safety failsafe",
        ["遥测新鲜度"] = "Telemetry freshness",
        ["最高速度设置"] = "Max speed setting",
        ["超速状态"] = "Overspeed status",
        ["限速保护"] = "Speed limit guard",
        ["输出转向"] = "Output steering",
        ["输出油门/刹车"] = "Output throttle/brake",
        ["输出状态"] = "Output status",
        ["停止输出"] = "Output stopped",
        ["正在写入"] = "Writing",
        ["帧率趋势（最近 60 个采样）"] = "Frame rate trend (last 60 samples)",
        ["最近：最低 {0} / 平均 {1} / 最高 {2} FPS"] = "Last: min {0} / avg {1} / max {2} FPS",
        ["遥测"] = "Telemetry",
        ["安全兜底"] = "Safety failsafe",
        ["当前速度"] = "Current speed",
        ["最高速度"] = "Max speed",
    };

    public static bool IsEnglish { get; private set; }

    public static event EventHandler? LanguageChanged;

    public static void SetEnglish(bool english)
    {
        if (english == IsEnglish) return;
        IsEnglish = english;
        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    public static string Translate(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value ?? string.Empty;
        if (!IsEnglish) return value;
        return Literals.TryGetValue(value, out var translated) ? translated : value;
    }
}