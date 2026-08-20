using Hexa.NET.ImGui;
using ETS2LA.Controls;
using ETS2LA.ML.Vision;
using ETS2LA.Shared.Localization;
using System.Numerics;

namespace ETS2LA.Overlay.Window;

class VisionCamerasWindow : InternalWindow
{
    public VisionCamerasWindow()
    {
        Definition = new WindowDefinition
        {
            Title = "视觉摄像头",
            Flags = ImGuiWindowFlags.AlwaysAutoResize,
        };

        IsWindowOpen = false;

        Render = () =>
        {
            unsafe
            {
                if (ImGui.BeginTable("CameraTable", 3, ImGuiTableFlags.NoPadInnerX))
                {
                    int cameraIndex = 0;
                    foreach (var camera in VisionHandler.Current.Cameras)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text($"{AppLocalization.Translate("摄像头")} {camera.Name} ({camera.Width}x{camera.Height})");
                        var texRef = new ImTextureRef(
                            texId: new ImTextureID((nint)camera.TextureId)
                        );

                        ImGui.Image(texRef, new Vector2(camera.Width, camera.Height));
                        cameraIndex++;
                    }
                    ImGui.EndTable();
                }
            }
        };
    }
}