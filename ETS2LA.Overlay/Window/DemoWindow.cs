using Hexa.NET.ImGui;

namespace ETS2LA.Overlay.Window;

class DemoWindow : InternalWindow
{
    public DemoWindow()
    {
        Definition = new WindowDefinition
        {
            Title = "演示窗口",
            NoWindow = true
        };

        IsWindowOpen = false;

        Render = () =>
        {
            ImGui.ShowDemoWindow();
        };
    }
}