using Hexa.NET.ImGui;
using ETS2LA.Controls;
using ETS2LA.State;
using ETS2LA.Shared.Localization;
using System.Numerics;

namespace ETS2LA.Overlay.Window;

class StateWindow : InternalWindow
{
    private void Text(string text)
    {
        ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.9f, 1f), text);
    }

    private void DescriptionText(string text)
    {
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), text);
    }
    
    private void ColoredBoolean(bool value, bool invert = false)
    {
        if (invert) value = !value;
        Vector4 color = value ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(1f, 0.5f, 0.5f, 1f);
        if (invert) value = !value; // revert back to original value for text
        ImGui.TextColored(color, $"{value}");
    }

    public StateWindow()
    {
        Definition = new WindowDefinition
        {
            Title = "状态信息",
            Flags = ImGuiWindowFlags.AlwaysAutoResize,
        };

        IsWindowOpen = false;

        Render = () =>
        {
            DescriptionText(AppLocalization.Translate("目标转向等级：")); ImGui.SameLine(); Text(ApplicationState.Current.DesiredSteeringLevel.ToString());

            DescriptionText(AppLocalization.Translate("暂停转向辅助：")); ImGui.SameLine(); ColoredBoolean(ApplicationState.Current.PauseSteeringAssist, invert: true);

            DescriptionText(AppLocalization.Translate("目标纵向等级：")); ImGui.SameLine(); Text(ApplicationState.Current.DesiredLongitudinalLevel.ToString());

            DescriptionText(AppLocalization.Translate("暂停纵向辅助：")); ImGui.SameLine(); ColoredBoolean(ApplicationState.Current.PauseLongitudinalAssist, invert: true);

            float speed = ApplicationState.Current.DesiredSpeed;
            Units displayUnits = ApplicationState.Current.DisplayUnits;
            float speedInUnits = UnitConversions.FromScientificUnits(UnitType.Speed, speed, displayUnits);
            string unitAbbreviation = UnitConversions.GetUnitAbbreviation(UnitType.Speed, displayUnits);
            DescriptionText(AppLocalization.Translate("目标速度：")); ImGui.SameLine(); Text($"{speed:F1} m/s ({speedInUnits:F1} in {unitAbbreviation})");

            DescriptionText(AppLocalization.Translate("显示单位：")); ImGui.SameLine(); Text(ApplicationState.Current.DisplayUnits.ToString());
        };
    }
}