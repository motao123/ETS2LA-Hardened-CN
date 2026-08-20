using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Avalonia.LogicalTree;
using ETS2LA.Settings;
using ETS2LA.UI.Settings;
using ETS2LA.Shared.Localization;

namespace ETS2LA.UI.Localization;

public enum UiLanguage
{
    ChineseSimplified,
    English
}

public static class LocalizationManager
{
    private static readonly Dictionary<string, string> LiteralEnglish = new(StringComparer.Ordinal)
    {
        ["主界面"] = "Main", ["仪表盘"] = "Dashboard", ["可视化"] = "Visualization", ["插件"] = "Plugins", ["管理器"] = "Manager", ["插件目录"] = "Catalogue", ["性能"] = "Performance", ["帮助"] = "Help", ["路线图"] = "Roadmap", ["设置"] = "Settings",
        ["驾驶"] = "Driving", ["常规"] = "General", ["其他"] = "Additional", ["辅助"] = "Assistance", ["辅助功能"] = "Assistance", ["数据"] = "Data", ["显示"] = "Display", ["音频"] = "Audio", ["主题"] = "Theme", ["控制"] = "Controls", ["开发包"] = "SDK", ["实验功能"] = "Experiments", ["用户"] = "User", ["更新"] = "Updates",
        ["启用匿名遥测"] = "Enable anonymous telemetry", ["颜色主题"] = "Color theme", ["强调色"] = "Accent color", ["系统"] = "System", ["浅色"] = "Light", ["深色"] = "Dark", ["中性"] = "Neutral", ["橙色"] = "Orange", ["音量"] = "Volume", ["当前音量"] = "Current volume", ["查看公开遥测数据"] = "View public telemetry data", ["此值需要重启后才能生效。"] = "This value requires a restart to take effect.",
        ["未知版本"] = "Unknown Version", ["切换侧栏"] = "Toggle sidebar", ["打开可视化"] = "Open visualization", ["打开插件管理器"] = "Open plugin manager", ["打开设置"] = "Open settings", ["切换透明度"] = "Toggle transparency", ["置于顶层"] = "Stay on top", ["最小化"] = "Minimize", ["关闭"] = "Close",
        ["主菜单"] = "Main menu", ["打开仪表盘"] = "Open dashboard", ["打开插件目录"] = "Open plugin catalogue", ["打开性能页面"] = "Open performance page", ["打开知识库（暂不可用）"] = "Open wiki (currently unavailable)", ["打开路线图（暂不可用）"] = "Open roadmap (currently unavailable)", ["打开性能页面"] = "Open performance page", ["目录"] = "Catalogue", ["知识库"] = "Wiki", ["最大化/还原"] = "Maximize/restore", ["最大化/还原窗口"] = "Maximize/restore window", ["最小化窗口"] = "Minimize window", ["关闭 ETS2LA"] = "Close ETS2LA",
        ["刷新目录"] = "Refresh catalogue", ["搜索插件..."] = "Search plugins...", ["需要重启！"] = "Restart required!", ["重启 ETS2LA"] = "Restart ETS2LA", ["重启 ETS2LA 以应用插件更改"] = "Restart ETS2LA to apply plugin changes", ["切换插件安装状态"] = "Toggle plugin installation", ["已安装"] = "Installed", ["有可用更新"] = "Update available", ["库"] = "Library", ["作者："] = "Author:", ["插件版本"] = "Plugin version", ["支持的 ETS2LA 版本"] = "Supported ETS2LA version", ["目录中未找到插件，请使用按钮刷新。你仍然可以手动安装插件。"] = "No plugins found in the catalogue. Use the refresh button. You can still install plugins manually.", ["打开插件文件夹"] = "Open plugin folder", ["卸载插件"] = "Unload plugins", ["重新加载插件"] = "Reload plugins", ["已启用"] = "Enabled", ["前往目录"] = "Go to catalogue", ["未找到插件，请确认插件文件夹中存在 .DLL 文件，或通过目录安装。"] = "No plugins found. Make sure the plugin folder contains .DLL files, or install one from the catalogue.",
        ["常见问题（支持）"] = "Frequently asked questions (support)", ["开发者文档"] = "Developer documentation", ["博客"] = "Blog", ["原始项目"] = "Original project", ["本项目基于 ETS2LA 开发，感谢原作者和所有贡献者。"] = "This project is based on ETS2LA. Thanks to the original authors and all contributors.", ["访问原作者项目"] = "Visit original project",
        ["开"] = "On", ["关"] = "Off", ["分离巡航与转向"] = "Separate cruise and steering", ["是否分离巡航控制和转向。分离后，单击将触发巡航，双击将同时启用两者。"] = "Whether to separate cruise control and steering. When separated, a single click triggers cruise and a double click enables both.", ["SET 行为"] = "SET behavior", ["按下 SET 按钮时，ETS2LA 应将当前巡航控制速度设置为什么？"] = "What should ETS2LA set the current cruise control speed to when SET is pressed?", ["按键行为"] = "Button behavior", ["将 ACC 吸附到 10 个单位"] = "Snap ACC to 10 units", ["更改速度时吸附到最接近的 10 个单位，例如 37 -> 39 -> 40 -> 42……"] = "Snap to the nearest 10 units when changing speed, for example 37 -> 39 -> 40 -> 42...", ["速度控制步长"] = "Speed control step", ["转向响应"] = "Steering response", ["加速响应"] = "Acceleration response", ["跟车距离"] = "Following distance", ["忽略交通规则"] = "Ignore traffic rules", ["最高速度"] = "Maximum speed", ["不稳定时暂停"] = "Pause when unstable", ["限速警告"] = "Speed limit warning", ["碰撞规避"] = "Collision avoidance", ["普通"] = "Normal", ["居中"] = "Centered", ["反向"] = "Inverted", ["拆分负向"] = "Split negative", ["拆分正向"] = "Split positive", ["未绑定"] = "Unbound", ["键盘"] = "Keyboard", ["按键"] = "Key", ["按钮"] = "Button", ["帽檐 "] = "Hat ",
        ["颜色主题"] = "Color theme", ["强调色"] = "Accent color", ["番茄红"] = "Tomato", ["红宝石红"] = "Ruby", ["深红"] = "Crimson", ["粉色"] = "Pink", ["梅红"] = "Plum", ["紫色"] = "Purple", ["紫罗兰"] = "Violet", ["鸢尾紫"] = "Iris", ["靛蓝"] = "Indigo", ["蓝色"] = "Blue", ["青色"] = "Cyan", ["蓝绿色"] = "Teal", ["翡翠绿"] = "Jade", ["绿色"] = "Green", ["草绿色"] = "Grass", ["青铜色"] = "Bronze", ["金色"] = "Gold", ["琥珀色"] = "Amber", ["黄色"] = "Yellow", ["青柠色"] = "Lime", ["薄荷色"] = "Mint", ["天蓝色"] = "Sky", ["当前版本："] = "Current version:", ["更新来源："] = "Update source:", ["检查更新"] = "Check for updates", ["最新版本："] = "Latest version:", ["安装并重启"] = "Install and restart", ["检查更新"] = "Check for updates", ["启用匿名遥测，需重启"] = "Enable anonymous telemetry, restart required", ["匿名遥测通过提供用户的系统规格、崩溃和其他使用数据来帮助我们改进 ETS2LA。您的数据无法通过任何个人信息识别。"] = "Anonymous telemetry helps improve ETS2LA with system specifications, crash reports, and usage data. Your data cannot be identified with personal information.", ["此值需要重启后才能生效。"] = "This value requires a restart to take effect.", ["查看公开遥测数据"] = "View public telemetry data",
        ["强制基础地图名称"] = "Force base map name", ["强制加载地图"] = "Force map loading", ["数据精度"] = "Data detail", ["曲线质量"] = "Curve quality", ["显示单位"] = "Display units", ["支持多个视口"] = "Support multiple viewports", ["限制覆盖层帧率"] = "Limit overlay frame rate", ["最大覆盖层帧率"] = "Maximum overlay frame rate", ["渲染 AR 元素"] = "Render AR elements", ["简化图形"] = "Simplified graphics", ["游戏暂停时停止渲染"] = "Stop rendering when game is paused", ["最大 AR 渲染距离"] = "Maximum AR render distance", ["渲染视觉摄像头"] = "Render vision cameras", ["点击卡片修改绑定。右键点击“普通轴”可修改其类型。"] = "Click a card to change its binding. Right-click a normal axis to change its type.", ["控制列表"] = "Controls list", ["检测到的游戏列表"] = "Detected games list", ["点击卡片安装或卸载 SDK。"] = "Click a card to install or uninstall the SDK.", ["已卸载"] = "Uninstalled", ["已安装"] = "Installed", ["失败"] = "Failed", ["找不到游戏？可以选择游戏安装文件夹手动添加。如果看到“bin”和“licenses”文件夹，就说明选择正确。"] = "Cannot find the game? Select its installation folder to add it manually. If you see 'bin' and 'licenses' folders, you selected the correct folder.", ["手动添加游戏"] = "Add game manually", ["输入版本"] = "Enter version", ["移除"] = "Remove",
        ["ACC 速度控制步长"] = "ACC speed control step", ["ETS2LA 使用 X11 输入。请在系统设置中查看 ETS2LA 可访问的按键（Legacy X11 App Support）。"] = "ETS2LA uses X11 input. Check the keys ETS2LA can access in system settings (Legacy X11 App Support).", ["ETS2LA 应在何时介入以避免碰撞？"] = "When should ETS2LA intervene to avoid collisions?", ["ETS2LA 应如何提醒限速？"] = "How should ETS2LA warn about speed limits?", ["ETS2LA 控制列表"] = "ETS2LA controls list", ["SET 按键行为"] = "SET button behavior",
        ["一些模组会实现 japan.mbd 或 russia.mbd 等额外地图。如果模组未正确加载，请尝试启用此选项。RusMap 需要此选项。"] = "Some mods add maps such as japan.mbd or russia.mbd. Enable this if a mod fails to load correctly. RusMap requires this option.", ["与公制单位只有细微差异，例如公制使用“bar”（大气压），科学单位使用“Pa”（帕斯卡）。"] = "Only slightly differs from metric units: metric uses 'bar' (atmosphere) while scientific uses 'Pa' (pascal).", ["使用简化图形可以提升低端硬件的性能，或解决不支持着色器等高级功能的情况。"] = "Simplified graphics improve performance on low-end hardware or work around missing shader support.", ["使用自适应巡航控制时，调整 ETS2LA 尝试与前车保持的距离。"] = "Adjust the distance ETS2LA tries to keep from the vehicle ahead when using adaptive cruise control.", ["允许 ETS2LA 在多个视口中渲染，因此可以将覆盖层窗口移动到任意显示器。"] = "Allows ETS2LA to render in multiple viewports so overlay windows can be moved to any monitor.", ["切换窗口置顶"] = "Toggle window stay on top", ["切换窗口透明度"] = "Toggle window transparency", ["刷新目录, icon-only"] = "Refresh catalogue, icon-only", ["大多数情况下，ETS2LA 超过一半的 CPU 占用来自覆盖层。可以降低覆盖层帧率以减少 CPU 和 GPU 使用率。"] = "In most cases, over half of ETS2LA's CPU usage comes from the overlay. Lower its frame rate to reduce CPU and GPU usage.", ["安装更新并重启"] = "Install and restart",
        ["强制 ETS2LA 在游戏启动时加载地图。这可以解决 game.log.txt 文件不在 ETS2LA 预期位置的问题。"] = "Force ETS2LA to load the map when the game starts. This fixes game.log.txt not being in the expected location.", ["强制基础地图名称，目前仅 RusMap 需要，需重启"] = "Force base map name; currently only needed for RusMap, requires restart", ["我们可以使用覆盖层渲染游戏世界中的元素，这会额外占用 CPU 和 GPU 资源。"] = "We can render elements in the game world using the overlay, at additional CPU and GPU cost.", ["我们将忽略交通规则和限速。请做好车辆及他人车辆受损的准备。为什么要启用此选项？"] = "We will ignore traffic rules and speed limits. Be prepared for potential damage. Why enable this?", ["所选文件夹中未找到 ETS2 或 ATS 可执行文件。请选择游戏安装文件夹，例如“.../steamapps/common/Euro Truck Simulator 2”。"] = "No ETS2 or ATS executable found in the selected folder. Choose the game installation folder, for example '.../steamapps/common/Euro Truck Simulator 2'.",
        ["打开 SDK 设置"] = "Open SDK settings", ["打开主题设置"] = "Open theme settings", ["打开实验功能设置"] = "Open experiments settings", ["打开控制设置"] = "Open controls settings", ["打开插件文件夹，仅图标按钮"] = "Open plugin folder, icon-only", ["打开插件目录按钮"] = "Open plugin catalogue, button", ["打开数据设置"] = "Open data settings", ["打开显示设置"] = "Open display settings", ["打开更新"] = "Open updates", ["打开用户设置"] = "Open user settings", ["打开辅助设置"] = "Open assistance settings", ["打开音频设置"] = "Open audio settings", ["数据完整度选择，较高等级使用更多内存"] = "Data detail selection; higher levels use more memory", ["无法添加游戏"] = "Could not add game", ["显示数值时应使用哪种单位？"] = "Which units should be used to display values?", ["最高速度限制"] = "Maximum speed limit", ["未安装"] = "Not installed",
        ["检测到不稳定，或转向无响应/自行禁用时，我们将暂停游戏。"] = "We will pause the game when instability is detected or steering becomes unresponsive or disables itself.", ["此设置仅加载游戏内地图中可见的道路和路口，并丢弃其他所有信息。"] = "Loads only roads and junctions visible on the in-game map and discards everything else.", ["此设置会删除冗余信息，同时保留大多数细节对象（例如路牌）。不包含游戏内地图未显示的道路或路口。"] = "Removes redundant information while keeping most detail objects (such as signs). Excludes roads and junctions not shown on the in-game map.", ["此设置会删除冗余信息，同时保留大多数细节对象（例如路牌）。包含所有隐藏道路和仅供 AI 使用的道路。"] = "Removes redundant information while keeping most detail objects (such as signs). Includes all hidden roads and AI-only roads.", ["此设置包含所有可用数据，其中大部分是冗余信息。"] = "Includes all available data, most of which is redundant.", ["渲染实验性视觉摄像头。这些摄像头可供基于视觉的机器学习模型用于端到端自动驾驶。请注意，这些模型目前尚不存在，但出于开发目的，我们保留了摄像头访问能力。"] = "Render experimental vision cameras available to vision-based ML models for end-to-end self-driving. These models do not exist yet, but camera access is kept for development.", ["游戏启动时强制加载地图，需重启"] = "Force map loading when the game starts, requires restart", ["游戏报告已暂停时，ETS2LA 不会渲染 AR 元素。"] = "ETS2LA will not render AR elements when the game reports it is paused.", ["游戏暂停时停止渲染 AR"] = "Stop rendering AR when the game is paused", ["由于内存不足，部分数据精度选项不可用。高需要 16GB，极高需要 20GB。"] = "Some data detail options are unavailable due to insufficient memory. High needs 16GB and Extreme needs 20GB.", ["碰撞规避距离"] = "Collision avoidance distance", ["窗口主题"] = "Window theme", ["窗口强调色"] = "Window accent color", ["红色"] = "Red", ["覆盖层支持多个视口"] = "Overlay supports multiple viewports", ["警告：ETS2LA 高度依赖 AR 元素，请仅在确实需要性能时禁用。"] = "Warning: ETS2LA heavily depends on AR elements. Disable only when you really need the performance.", ["警告：极高会占用大量内存，预计使用超过 10GB。"] = "Warning: Extreme uses a lot of memory, over 10GB.", ["警告：需要重启 ETS2LA 才能应用此更改。"] = "Warning: ETS2LA must be restarted for this change to take effect.", ["调整 ETS2LA 对必要转向变化的响应方式，例如控制变道速度。"] = "Adjusts how ETS2LA responds to necessary steering changes, such as lane change speeds.", ["调整 ETS2LA 对期望加速度变化的响应速度。紧急制动不受影响。"] = "Adjusts how quickly ETS2LA responds to desired acceleration changes. Emergency braking is unaffected.", ["调整所使用游戏数据的详细程度。数值越高，内存占用越大，但包含的信息也越多。所有级别的驾驶效果相同。"] = "Adjusts the detail level of game data used. Higher values use more memory but include more information. Driving behavior is identical at every level.", ["调整路径规划和可视化所用曲线的质量。默认设置与游戏一致。"] = "Adjusts the quality of curves used for path planning and visualization. The default matches the game.", ["重新加载插件，仅图标按钮"] = "Reload plugins, icon-only", ["卸载插件，仅图标按钮"] = "Unload plugins, icon-only", ["限速警告样式"] = "Speed limit warning style", ["需要重启 ETS2LA 才能应用此选项。"] = "ETS2LA must be restarted to apply this option.",
        ["加载文件出错"] = "Error loading file", ["正在解压模组"] = "Unpacking mods", ["加载地图数据出错"] = "Error loading map data", ["正在解析地图数据"] = "Parsing map data", ["正在初始化……"] = "Initializing...", ["地图数据解析完成"] = "Map data parsed", ["用户界面简介"] = "Introduction to the user interface", ["教程完成"] = "Tutorial finished", ["覆盖层简介"] = "Introduction to the overlay", ["游戏遥测"] = "Game telemetry", ["无法连接到游戏。请打开 ETS2 或 ATS 并启用 SDK。"] = "Unable to connect to the game. Open ETS2 or ATS and enable the SDK."
    };

    private static readonly Dictionary<UiLanguage, IReadOnlyDictionary<string, string>> Resources = new()
    {
        [UiLanguage.ChineseSimplified] = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AppTitle"] = "ETS2LA 安全加固版",
            ["UnknownVersion"] = "未知版本",
            ["Main"] = "主界面",
            ["Dashboard"] = "仪表盘",
            ["Visualization"] = "可视化",
            ["Plugins"] = "插件",
            ["Manager"] = "管理器",
            ["Catalogue"] = "插件目录",
            ["Performance"] = "性能",
            ["Help"] = "帮助",
            ["Wiki"] = "Wiki",
            ["Roadmap"] = "路线图",
            ["Settings"] = "设置",
            ["Language"] = "界面语言",
            ["ChineseSimplified"] = "简体中文",
            ["English"] = "English",
            ["LanguageRestart"] = "切换语言后需要重启程序。",
            ["WelcomeBack"] = "欢迎回来，{0}！",
            ["CurrentRelease"] = "当前版本",
            ["UsersOnline"] = "在线用户",
            ["UsersOver24h"] = "24 小时活跃用户",
            ["SupportUpdates"] = "支持与更新",
            ["SupportUpdatesDescription"] = "关注项目开发并获取支持。此版本仍在开发中，如发现问题请提交反馈。",
            ["Documentation"] = "文档",
            ["SponsorUs"] = "支持项目",
            ["SponsorDescription"] = "你的支持可以帮助项目持续开发，让我有更多时间改进 ETS2LA。谢谢！",
            ["Donate"] = "捐赠支持",
            ["Driving"] = "驾驶",
            ["General"] = "常规",
            ["Additional"] = "其他",
            ["Assistance"] = "辅助功能",
            ["Data"] = "数据",
            ["Display"] = "显示",
            ["Audio"] = "音频",
            ["Theme"] = "主题",
            ["Controls"] = "控制",
            ["Experiments"] = "实验功能",
            ["User"] = "用户",
            ["Updates"] = "更新",
            ["TelemetryEnabled"] = "启用匿名遥测",
            ["TelemetryDescription"] = "匿名遥测通过提供用户的系统规格、崩溃和其他使用数据来帮助我们改进 ETS2LA。您的数据无法通过任何个人信息识别。",
            ["RestartRequired"] = "此值需要重启后才能生效。",
            ["PublicTelemetry"] = "查看公开遥测数据",
            ["StayOnTop"] = "置于顶层",
            ["Transparency"] = "透明度",
            ["Enabled"] = "已启用",
            ["Disabled"] = "已禁用",
            ["ShuttingDown"] = "正在关闭应用程序和后端……",
            ["BindingControl"] = "控制绑定",
            ["BindingPrompt"] = "请按键、按钮或移动轴来绑定“{0}”",
            ["BindingSucceeded"] = "控制已绑定",
            ["BindingSucceededContent"] = "已成功将“{0}”绑定到 {1} - {2}",
            ["BindingCancelled"] = "绑定已取消",
            ["BindingCancelledContent"] = "“{0}”的绑定已取消或超时。",
            ["CheckingUpdates"] = "正在检查更新",
            ["CheckingUpdatesContent"] = "请稍候，正在检查更新……",
            ["UpdateAvailable"] = "有可用更新",
            ["UpdateAvailableContent"] = "有新版本可用：{0}",
            ["NoUpdates"] = "没有可用更新",
            ["NoUpdatesContent"] = "您正在使用最新版本。",
            ["DownloadingUpdate"] = "正在下载更新",
            ["DownloadProgress"] = "下载进度：{0}%",
            ["StartingDownload"] = "开始下载……",
            ["NoReleaseNotes"] = "没有可用的发行说明。",
            ["SDKUninstalledTitle"] = "已卸载 {0} 的 SDK",
            ["SDKUninstalledContent"] = "已成功卸载 {0} 在 {1} 的 SDK。",
            ["SDKUninstallFailedTitle"] = "卸载 {0} 的 SDK 失败",
            ["SDKUninstallFailedContent"] = "卸载 {0} 在 {1} 的 SDK 时出错。请查看日志了解详情。",
            ["SDKInstalledTitle"] = "已安装 {0} 的 SDK",
            ["SDKInstalledContent"] = "已成功安装 {0} 在 {1} 的 SDK。",
            ["SDKInstallFailedTitle"] = "安装 {0} 的 SDK 失败",
            ["SDKInstallFailedContent"] = "安装 {0} 在 {1} 的 SDK 时出错。请查看日志了解详情。",
            ["SelectGameFolder"] = "选择游戏安装文件夹",
            ["CouldNotAddGame"] = "无法添加游戏",
            ["NoGameExecutableFound"] = "所选文件夹中未找到 ETS2 或 ATS 可执行文件。请选择游戏安装文件夹，例如“.../steamapps/common/Euro Truck Simulator 2”。",
            ["VisualizationPlaceholderTitle"] = "抱歉",
            ["VisualizationPlaceholderBody"] = "此页面正在重做，当前版本暂不可用，将在后续更新中回归。",
            ["PerformancePlaceholderTitle"] = "性能",
            ["PerformancePlaceholderBody"] = "此页面尚未实现，你可以使用外部工具监控性能。",
            ["RoadmapPlaceholderTitle"] = "路线图",
            ["RoadmapPlaceholderBody"] = "请前往 GitHub 仓库的 Projects 标签页查看公开路线图。",
            ["PortableUpdateTitle"] = "便携版",
            ["PortableUpdateContent"] = "便携版不支持应用内自动更新，请前往 GitHub Releases 手动下载最新版本。",
            ["ManualDownload"] = "手动下载更新"
        },
        [UiLanguage.English] = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AppTitle"] = "ETS2LA Hardened CN",
            ["UnknownVersion"] = "Unknown Version",
            ["Main"] = "Main",
            ["Dashboard"] = "Dashboard",
            ["Visualization"] = "Visualization",
            ["Plugins"] = "Plugins",
            ["Manager"] = "Manager",
            ["Catalogue"] = "Catalogue",
            ["Performance"] = "Performance",
            ["Help"] = "Help",
            ["Wiki"] = "Wiki",
            ["Roadmap"] = "Roadmap",
            ["Settings"] = "Settings",
            ["ChineseSimplified"] = "简体中文",
            ["English"] = "English",
            ["LanguageRestart"] = "Language changes are applied immediately.",
            ["WelcomeBack"] = "Welcome back, {0}!",
            ["CurrentRelease"] = "Current release",
            ["UsersOnline"] = "Users online",
            ["UsersOver24h"] = "Users over 24 hours",
            ["SupportUpdates"] = "Support and updates",
            ["SupportUpdatesDescription"] = "Follow development and get support. This version is still under development; please report any issues.",
            ["Documentation"] = "Documentation",
            ["SponsorUs"] = "Support the project",
            ["SponsorDescription"] = "Your support helps continued development and gives me more time to improve ETS2LA. Thank you!",
            ["Donate"] = "Donate",
            ["Driving"] = "Driving", ["General"] = "General", ["Additional"] = "Additional", ["Assistance"] = "Assistance", ["Data"] = "Data", ["Display"] = "Display", ["Audio"] = "Audio", ["Theme"] = "Theme", ["Controls"] = "Controls", ["Experiments"] = "Experiments", ["User"] = "User", ["Updates"] = "Updates",
            ["TelemetryEnabled"] = "Enable anonymous telemetry",
            ["TelemetryDescription"] = "Anonymous telemetry helps us improve ETS2LA by providing system specifications, crashes, and other usage data. Your data cannot be tied to personal information.",
            ["RestartRequired"] = "This value requires a restart to take effect.",
            ["PublicTelemetry"] = "View public telemetry data",
            ["StayOnTop"] = "Stay on top", ["Transparency"] = "Transparency", ["Enabled"] = "Enabled", ["Disabled"] = "Disabled", ["ShuttingDown"] = "Shutting down application and backend...",
            ["BindingControl"] = "Control binding", ["BindingPrompt"] = "Press a key or button, or move an axis, to bind '{0}'", ["BindingSucceeded"] = "Control bound", ["BindingSucceededContent"] = "Successfully bound '{0}' to {1} - {2}", ["BindingCancelled"] = "Binding cancelled", ["BindingCancelledContent"] = "Binding '{0}' was cancelled or timed out.",
            ["CheckingUpdates"] = "Checking for updates", ["CheckingUpdatesContent"] = "Please wait while updates are checked...", ["UpdateAvailable"] = "Update available", ["UpdateAvailableContent"] = "A new version is available: {0}", ["NoUpdates"] = "No updates available", ["NoUpdatesContent"] = "You are running the latest version.", ["DownloadingUpdate"] = "Downloading update", ["DownloadProgress"] = "Download progress: {0}%", ["StartingDownload"] = "Starting download...", ["NoReleaseNotes"] = "No release notes available.",
            ["SDKUninstalledTitle"] = "Uninstalled SDK for {0}",
            ["SDKUninstalledContent"] = "Successfully uninstalled the SDK for {0} at {1}.",
            ["SDKUninstallFailedTitle"] = "Failed to uninstall SDK for {0}",
            ["SDKUninstallFailedContent"] = "Failed to uninstall the SDK for {0} at {1}. Check the logs for details.",
            ["SDKInstalledTitle"] = "Installed SDK for {0}",
            ["SDKInstalledContent"] = "Successfully installed the SDK for {0} at {1}.",
            ["SDKInstallFailedTitle"] = "Failed to install SDK for {0}",
            ["SDKInstallFailedContent"] = "Failed to install the SDK for {0} at {1}. Check the logs for details.",
            ["SelectGameFolder"] = "Select the game installation folder",
            ["CouldNotAddGame"] = "Could not add game",
            ["NoGameExecutableFound"] = "No ETS2 or ATS executable found in the selected folder. Choose the game installation folder, for example '.../steamapps/common/Euro Truck Simulator 2'.",
            ["VisualizationPlaceholderTitle"] = "Sorry",
            ["VisualizationPlaceholderBody"] = "This page is being remade and isn't available in this version. It will return in a future update.",
            ["PerformancePlaceholderTitle"] = "Performance",
            ["PerformancePlaceholderBody"] = "This page hasn't been implemented yet; you can monitor performance using external tools.",
            ["RoadmapPlaceholderTitle"] = "Roadmap",
            ["RoadmapPlaceholderBody"] = "Please take a look at our public roadmap on GitHub. Navigate to the repository and click on the Projects tab at the top.",
            ["PortableUpdateTitle"] = "Portable build",
            ["PortableUpdateContent"] = "Portable builds do not support in-app auto-update. Please download the latest version from GitHub Releases manually.",
            ["ManualDownload"] = "Download update manually"
        }
    };

    public static UiLanguage Current
    {
        get
        {
            var value = UISettingsHandler.Current.GetSettings().Language;
            var language = Enum.TryParse<UiLanguage>(value, out var parsed) ? parsed : UiLanguage.ChineseSimplified;
            AppLocalization.SetEnglish(language == UiLanguage.English);
            return language;
        }
    }

    public static string Get(string key)
    {
        if (Resources[Current].TryGetValue(key, out var value)) return value;
        return Resources[UiLanguage.English].TryGetValue(key, out var fallback) ? fallback : key;
    }

    public static string Format(string key, params object?[] args) => string.Format(Get(key), args);

    public static string TranslateLiteral(string? value)
    {
        if (string.IsNullOrEmpty(value) || Current == UiLanguage.ChineseSimplified) return value ?? string.Empty;
        return LiteralEnglish.TryGetValue(value, out var translated) ? translated : AppLocalization.Translate(value);
    }

    private static readonly Dictionary<Control, ControlLocalizationState> States = new();
    private static readonly HashSet<Control> Roots = new(ReferenceEqualityComparer.Instance);

    public static void Localize(Control root)
    {
        Roots.Add(root);
        LocalizeControl(root);
        foreach (var control in root.GetLogicalDescendants().OfType<Control>()) LocalizeControl(control);
        foreach (var control in root.GetVisualDescendants().OfType<Control>()) LocalizeControl(control);
    }

    private static void LocalizeControl(Control control)
    {
        if (!States.TryGetValue(control, out var state))
        {
            state = new ControlLocalizationState(
                control is TextBlock textBlock ? textBlock.Text : null,
                control is ContentControl { Content: string content } ? content : null,
                ToolTip.GetTip(control) as string,
                AutomationProperties.GetName(control));
            States[control] = state;
        }

        if (control is TextBlock text) text.Text = TranslateLiteral(state.Text);
        if (control is ContentControl contentControl && state.Content is not null)
            contentControl.Content = TranslateLiteral(state.Content);
        if (state.ToolTip is not null) ToolTip.SetTip(control, TranslateLiteral(state.ToolTip));
        if (state.AutomationName is not null) AutomationProperties.SetName(control, TranslateLiteral(state.AutomationName));
    }

    private sealed record ControlLocalizationState(string? Text, string? Content, string? ToolTip, string? AutomationName);

    public static void Set(UiLanguage language)
    {
        if (language == Current) return;
        UISettingsHandler.Current.GetSettings().Language = language.ToString();
        UISettingsHandler.Current.Save();
        AppLocalization.SetEnglish(language == UiLanguage.English);
        LanguageChanged?.Invoke(null, EventArgs.Empty);
        foreach (var root in Roots.ToList()) Localize(root);
    }

    public static event EventHandler? LanguageChanged;
}

public sealed class LanguageOption
{
    public required UiLanguage Value { get; init; }
    public required string DisplayName { get; init; }

    public override string ToString() => DisplayName;
}
