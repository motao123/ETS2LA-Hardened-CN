using Hexa.NET.ImGui;
using ETS2LA.Game.Output;
using ETS2LA.Game.Telemetry;
using ETS2LA.Shared.Localization;

namespace ETS2LA.Overlay.Window;

class SafetyStatusWindow : InternalWindow
{
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

        Render = () =>
        {
            var fresh = GameTelemetry.Current.IsFresh(TimeSpan.FromSeconds(2));
            ImGui.Text($"{AppLocalization.Translate("遥测")}: {AppLocalization.Translate(fresh ? "新鲜" : "陈旧（已复位输出）")}");

            var output = GameOutput.Current.LastOutput;
            ImGui.Text($"{AppLocalization.Translate("输出转向")}: {output.Steering:F2}");
            ImGui.Text($"{AppLocalization.Translate("输出油门/刹车")}: {output.Acceleration:F2}");
            ImGui.Text($"{AppLocalization.Translate("输出状态")}: {AppLocalization.Translate(output.IsStale(TimeSpan.FromSeconds(1)) ? "停止输出" : "正在写入")}");
        };
    }
}