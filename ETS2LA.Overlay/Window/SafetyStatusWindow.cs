using Hexa.NET.ImGui;
using ETS2LA.Game.Output;
using ETS2LA.Game.Telemetry;
using ETS2LA.Settings.Global;
using ETS2LA.Shared.Localization;
using System.Numerics;

namespace ETS2LA.Overlay.Window;

/// <summary>
/// 游戏内覆盖层「安全兜底」HUD：状态颜色化，方便开车时一眼看到
/// 遥测新鲜度、超速/限速保护、以及主机当前是否在写入控制。
/// </summary>
class SafetyStatusWindow : InternalWindow
{
    private static readonly Vector4 Green = new(0.5f, 1f, 0.5f, 1f);
    private static readonly Vector4 Orange = new(1f, 0.8f, 0.3f, 1f);
    private static readonly Vector4 Red = new(1f, 0.5f, 0.5f, 1f);
    private static readonly Vector4 Gray = new(0.7f, 0.7f, 0.7f, 1f);

    public SafetyStatusWindow()
    {
        Definition = new WindowDefinition
        {
            Title = "安全兜底",
            Flags = ImGuiWindowFlags.AlwaysAutoResize,
            X = 10,
            Y = 70,
            Alpha = 0.85f
        };

        IsWindowOpen = false;

        Render = RenderSafety;
    }

    private static void RenderSafety()
    {
        var fresh = GameTelemetry.Current.IsFresh(TimeSpan.FromSeconds(2));
        ColoredText("遥测", fresh ? Green : Red, fresh ? "新鲜" : "陈旧（已复位输出）");

        var data = GameTelemetry.Current.GetCurrentData();
        var speedMps = data?.truckFloat.speed ?? 0f;
        var maxMps = AssistanceSettings.Current.MaximumSpeed;
        ImGui.Text($"{AppLocalization.Translate("当前速度")}: {speedMps * 3.6f:F0} km/h");
        ImGui.Text($"{AppLocalization.Translate("最高速度")}: {(maxMps > 0f ? $"{maxMps * 3.6f:F0} km/h" : AppLocalization.Translate("不限速"))}");

        if (maxMps > 0f && speedMps > maxMps)
        {
            var guarded = SpeedLimitGuard.LimitAcceleration(speedMps, maxMps, acceleration: 1f);
            ColoredText("限速保护", guarded < 0f ? Red : Orange, guarded < 0f ? "正在制动" : "禁止油门");
        }
        else
        {
            ColoredText("限速保护", Green, "未生效");
        }

        var output = GameOutput.Current.LastOutput;
        ImGui.Text($"{AppLocalization.Translate("输出转向")}: {output.Steering:F2}");
        ImGui.Text($"{AppLocalization.Translate("输出油门/刹车")}: {output.Acceleration:F2}");
        var writing = !output.IsStale(TimeSpan.FromSeconds(1));
        ColoredText("输出状态", writing ? Green : Gray, writing ? "正在写入" : "停止输出");
    }

    private static void ColoredText(string labelKey, Vector4 color, string valueKey)
    {
        ImGui.TextColored(color, $"{AppLocalization.Translate(labelKey)}: {AppLocalization.Translate(valueKey)}");
    }
}