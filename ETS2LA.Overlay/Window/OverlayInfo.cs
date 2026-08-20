using Hexa.NET.ImGui;
using ETS2LA.Controls;
using ETS2LA.Shared.Localization;
using System.Numerics;

namespace ETS2LA.Overlay.Window;

class OverlayInfoWindow : InternalWindow
{
    public OverlayInfoWindow()
    {
        Definition = new WindowDefinition
        {
            Title = "叠加层信息",
            Flags = ImGuiWindowFlags.AlwaysAutoResize,
        };

        IsWindowOpen = false;

        Render = () =>
        {
            ImGui.Text(AppLocalization.Translate("*惊喜* 这里有一个新窗口 O_O"));
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), AppLocalization.Translate("这是最终会在游戏上方显示信息的叠加层。对于 C#，我们实际上已经让它比以前强大得多！"));
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), AppLocalization.Translate("插件开发者现在可以完全访问 ImGui 进行渲染，希望能看到一些有趣的成果！"));
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), AppLocalization.Translate("目前我们只实现了基础功能，遥测插件将展示在渲染大量数据时不错的性能。"));
            ImGui.Separator();
            ImGui.Text(AppLocalization.Translate("按住以下按键即可与叠加层交互"));
            ImGui.SameLine();
            var controls = ControlsBackend.Current.GetRegisteredControls();        
            var interactKey = controls.FirstOrDefault(c => c.Definition.Id == OverlayHandler.Current.Interact.Id);

            ImGui.PushFont(OverlayHandler.Current.Fonts[FontStyle.Bold], 18f);
            if (interactKey != null)
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), interactKey.ControlId.ToString());
            else 
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), AppLocalization.Translate("未绑定"));
            ImGui.PopFont();
            
            ImGui.SameLine();
            ImGui.Text(AppLocalization.Translate("（可在设置中修改！）"));
            ImGui.Text(AppLocalization.Translate("叠加层基本上是一个完整的窗口系统，理论上不应该发生崩溃……希望如此。如果真的发生了，请报告问题！"));
        };
    }
}