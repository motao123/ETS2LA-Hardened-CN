namespace ETS2LA.Controls.Defaults;

public static class DefaultControls
{
    public static ControlDefinition Assist { get; } = new ControlDefinition
    {
        Id = "ETS2LA.Controls.Assist",
        Name = "Assist",
        Description = "切换 ETS2LA 的辅助功能开关。不会更新速度，如需更新速度请使用 SET。你可以在辅助功能设置中更改此按键（以及 SET）的行为。",
        DefaultKeybind = "N",
        Type = ControlType.Boolean
    };

    public static ControlDefinition SET { get; } = new ControlDefinition
    {
        Id = "ETS2LA.Controls.SET",
        Name = "SET/OK",
        Description = "功能类似 Assist，但会按照你在辅助功能设置中的选择执行。此按键还用于确认操作。",
        DefaultKeybind = "Left",
        Type = ControlType.Boolean
    };

    public static ControlDefinition Next { get; } = new ControlDefinition
    {
        Id = "ETS2LA.Controls.Next",
        Name = "Next/Cancel",
        Description = "此按键可在 ETS2LA 菜单中前进，也可在确认操作中用作取消键。",
        DefaultKeybind = "Right",
        Type = ControlType.Boolean
    };

    public static ControlDefinition Increase { get; } = new ControlDefinition
    {
        Id = "ETS2LA.Controls.Increase",
        Name = "Increase",
        Description = "将当前数值（例如目标速度）增加一个步长。如果界面中没有显示其他修正值，目标速度将增加 1 km/h。",
        DefaultKeybind = "Up",
        Type = ControlType.Boolean
    };

    public static ControlDefinition Decrease { get; } = new ControlDefinition
    {
        Id = "ETS2LA.Controls.Decrease",
        Name = "Decrease",
        Description = "将当前数值（例如目标速度）减少一个步长。如果界面中没有显示其他修正值，目标速度将减少 1 km/h。",
        DefaultKeybind = "Down",
        Type = ControlType.Boolean
    };

}