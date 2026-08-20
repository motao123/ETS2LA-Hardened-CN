using Hexa.NET.ImGui;
using ETS2LA.Overlay;
using ETS2LA.Controls;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System.Numerics;
using ETS2LA.Backend.Events;
using ETS2LA.Shared.Localization;

namespace ETS2LA.Tutorials.DefaultTutorials;

public class OnboardingPart2
{
    bool hasMoved = false;

    public Tutorial Create()
    {
        return new Tutorial("OnboardingPart2", AppLocalization.Translate("从插件目录安装完成后的入门教程。"), "ETS2LA", new List<TutorialSection>
        {
            new TutorialSection
            {
                Title = AppLocalization.Translate("用户界面简介"),
                Actions = new List<TutorialAction>
                {
                    new ShowMessageAction
                    {
                        Message = AppLocalization.Translate("很好！\n接下来我们去设置。"),
                        ScreenPositionCallback = () =>
                        {
                            var position = ETS2LAWindowLocation();
                            var size = ETS2LAWindowSize();
                            return (position.Item1 + 220, position.Item2 + size.Item2 - 70);
                        }
                    },
                    new WaitForEventAction
                    {
                        EventId = "ETS2LA.UI.SwitchedPage.Settings"
                    },
                    new ShowMessageAction
                    {
                        Message = AppLocalization.Translate("我们来看看控制设置。"),
                        ScreenPositionCallback = () =>
                        {
                            var position = ETS2LAWindowLocation();
                            var size = ETS2LAWindowSize();
                            return (position.Item1 + 220, position.Item2 + 320);
                        }
                    },
                    new WaitForEventAction
                    {
                        EventId = "ETS2LA.UI.SwitchedPage.Settings.Controls"
                    },
                    new ExecuteFunctionAction
                    {
                        Function = () =>
                        {
                            OverlayHandler.Current.OpenWindow("State Info");
                        }
                    },
                    new ShowMessageAction
                    {
                        Message = AppLocalization.Translate("你可以在这里查看 ETS2LA 的所有控制。我们还打开了“状态信息”窗口，方便你查看当前设置。\n请按下“SET”切换自适应巡航控制。"),
                        ScreenPositionCallback = () =>
                        {
                            var position = ETS2LAWindowLocation();
                            var size = ETS2LAWindowSize();
                            return (position.Item1 + 442, position.Item2 + 3);
                        }
                    },
                    new WaitForEventAction
                    {
                        EventId="ETS2LA.State.AssistsPaused"
                    },
                    new ShowMessageAction
                    {
                        Message = AppLocalization.Translate("你可以在这里查看 ETS2LA 的所有控制。我们还打开了“状态信息”窗口，方便你查看当前设置。\n你也可以使用“ASSIST”在不同转向模式之间切换。"),
                        ScreenPositionCallback = () =>
                        {
                            var position = ETS2LAWindowLocation();
                            var size = ETS2LAWindowSize();
                            return (position.Item1 + 442, position.Item2 + 3);
                        }
                    },
                    new WaitForEventAction
                    {
                        EventId="ETS2LA.State.SteeringLevel.None"
                    },
                    new ShowMessageAction
                    {
                        Message = AppLocalization.Translate("很好！\n接下来我们去插件管理器页面。"),
                        ScreenPositionCallback = () =>
                        {
                            var position = ETS2LAWindowLocation();
                            var size = ETS2LAWindowSize();
                            return (position.Item1 + 15, position.Item2 + 202);
                        }
                    },
                    new WaitForEventAction
                    {
                        EventId = "ETS2LA.UI.SwitchedPage.Manager"
                    },
                    new ShowMessageAction
                    {
                        Message = AppLocalization.Translate("这里显示了你已安装的所有插件。首先启用“车道辅助”和“自适应巡航控制”插件。"),
                        ScreenPositionCallback = () =>
                        {
                            var position = ETS2LAWindowLocation();
                            var size = ETS2LAWindowSize();
                            return (position.Item1 + 230, position.Item2 + 1);
                        }
                    },
                    new WaitForEventAction
                    {
                        EventId = "ETS2LA.Backend.Enabled.tumppi066.adaptivecruisecontrol"
                    },
                    new ShowMessageWaitNextAction
                    {
                        Message = AppLocalization.Translate("入门教程到此结束。如有疑问，请查看我们的 YouTube 频道和 Discord 获取更多信息。\n仪表盘页面中可以找到所有链接。"),
                        ScreenPositionCallback = () =>
                        {
                            var position = ETS2LAWindowLocation();
                            var size = ETS2LAWindowSize();
                            return (position.Item1 + 230, position.Item2 + 3);
                        }
                    },
                    new SendNotificationAction
                    {
                        Title = AppLocalization.Translate("教程完成"),
                        Message = AppLocalization.Translate("欢迎使用 ETS2LA！"),
                        Level = Notifications.NotificationLevel.Success,
                        CloseAfter = 5f
                    },
                }
            }
        });
    }

    private void AlignForWidth(float width, float alignment = 0.5f)
    {
        float avail = ImGui.GetContentRegionAvail().X;
        float off = (avail - width) * alignment;
        if (off > 0.0f)
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + off);
    }

    private void WelcomePage()
    {
        // Pad top
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 100);

        ImGui.PushFont(OverlayHandler.Current.Fonts[FontStyle.Bold], 20);
        AlignForWidth(ImGui.CalcTextSize(AppLocalization.Translate("欢迎使用 ETS2LA！")).X);
        ImGui.Text(AppLocalization.Translate("欢迎使用 ETS2LA！"));
        ImGui.Spacing();
        ImGui.PopFont();

        AlignForWidth(ImGui.CalcTextSize(AppLocalization.Translate("让我们先熟悉一下用户界面。")).X);
        ImGui.Text(AppLocalization.Translate("让我们先熟悉一下用户界面。"));
        AlignForWidth(ImGui.CalcTextSize(AppLocalization.Translate("你现在看到的窗口是覆盖层。")).X);
        ImGui.Text(AppLocalization.Translate("你现在看到的窗口是覆盖层。"));
        ImGui.Spacing();
        ImGui.Spacing();

        AlignForWidth(ImGui.CalcTextSize(AppLocalization.Translate("按住覆盖层交互键即可继续。")).X);
        ImGui.Text(AppLocalization.Translate("按住覆盖层交互键即可继续。"));

        # if LINUX
        AlignForWidth(ImGui.CalcTextSize("Note: You're on Linux, make sure you are allowing X11 global hotkeys on keys you need.").X);
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), "Note: You're on Linux, make sure you are allowing X11 global hotkeys on keys you need.");
        ImGui.Spacing();
        ImGui.Spacing();
        # endif

        var controls = ControlsBackend.Current.GetRegisteredControls();        
        var interactKey = controls.FirstOrDefault(c => c.Definition.Id == OverlayHandler.Current.Interact.Id);
        var text = interactKey != null ? interactKey.ControlId.ToString() : "UNBOUND";

        AlignForWidth(ImGui.CalcTextSize(text).X);
        ImGui.PushFont(OverlayHandler.Current.Fonts[FontStyle.Bold], 18f);
        ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), text);
        ImGui.PopFont();
    }

    private void OverlayInteractionPage()
    {
        if (!hasMoved)
        {
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                Events.Current.Publish("Onboarding.MovedWindow", new EventArgs());
                hasMoved = true;
            }
        }

        // Pad top
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 100);

        if (!hasMoved)
        {
            ImGui.PushFont(OverlayHandler.Current.Fonts[FontStyle.Bold], 20);
            AlignForWidth(ImGui.CalcTextSize(AppLocalization.Translate("很好！")).X);
            ImGui.Text(AppLocalization.Translate("很好！"));
            ImGui.Spacing();
            ImGui.PopFont();

            AlignForWidth(ImGui.CalcTextSize(AppLocalization.Translate("这个覆盖层用于 ETS2LA 的许多功能。")).X);
            ImGui.Text(AppLocalization.Translate("这个覆盖层用于 ETS2LA 的许多功能。"));
            AlignForWidth(ImGui.CalcTextSize(AppLocalization.Translate("如果你不喜欢当前按键绑定，之后可以在设置中修改。")).X);
            ImGui.Text(AppLocalization.Translate("如果你不喜欢当前按键绑定，之后可以在设置中修改。"));
            ImGui.Spacing();
            ImGui.Spacing();

            AlignForWidth(ImGui.CalcTextSize(AppLocalization.Translate("现在已进入覆盖层模式，你可以与窗口交互。")).X);
            ImGui.Text(AppLocalization.Translate("现在已进入覆盖层模式，你可以与窗口交互。"));

            AlignForWidth(ImGui.CalcTextSize(AppLocalization.Translate("试着拖动这个窗口来移动它！")).X);
            ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), AppLocalization.Translate("试着拖动这个窗口来移动它！"));
            ImGui.Spacing();
            ImGui.Spacing();
        }

        if (hasMoved)
        {
            ImGui.PushFont(OverlayHandler.Current.Fonts[FontStyle.Bold], 20);
            AlignForWidth(ImGui.CalcTextSize(AppLocalization.Translate("太棒了！")).X);
            ImGui.Text(AppLocalization.Translate("太棒了！"));
            ImGui.Spacing();
            ImGui.PopFont();
            
            AlignForWidth(ImGui.CalcTextSize(AppLocalization.Translate("记住，如需与覆盖层窗口交互，请先进入交互模式！")).X);
            ImGui.Text(AppLocalization.Translate("记住，如需与覆盖层窗口交互，请先进入交互模式！"));
            ImGui.Spacing();

            AlignForWidth(ImGui.CalcTextSize(AppLocalization.Translate("退出覆盖层交互模式即可继续。")).X);
            ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), AppLocalization.Translate("退出覆盖层交互模式即可继续。"));
        }
    }

    private (int, int) ETS2LAWindowLocation()
    {
        if (Application.Current == null || Application.Current.ApplicationLifetime == null)
            return (0, 0);

        var window = ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).MainWindow;
        if (window == null)
            return (0, 0);
        
        return (window.Position.X, window.Position.Y);
    }

    private (int, int) ETS2LAWindowSize()
    {
        if (Application.Current == null || Application.Current.ApplicationLifetime == null)
            return (0, 0);

        var window = ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).MainWindow;
        if (window == null || window.FrameSize == null)
            return (0, 0);
        
        var size = ((int)window.FrameSize.Value.Width, (int)window.FrameSize.Value.Height);
        return size;
    }
}