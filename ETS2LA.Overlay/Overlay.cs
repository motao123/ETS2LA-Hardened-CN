// Much of this file is based on the Hexa.NET.ImGui example code. See the relevant example here:
// https://github.com/HexaEngine/Hexa.NET.ImGui/blob/main/Examples/ExampleGLFWOpenGL3/Program.cs

using Hexa.NET.GLFW;
using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;
using Hexa.NET.ImGui.Backends.GLFW;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Hexa.NET.OpenGL;
using HexaGen.Runtime;
using GLFWwindowPtr = Hexa.NET.GLFW.GLFWwindowPtr;

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Numerics;
using System.Diagnostics;
using Avalonia.Data;

using ETS2LA.Logging;
using ETS2LA.Controls;
using ETS2LA.Overlay.Window;
using ETS2LA.Overlay.AR;
using ETS2LA.ML;
using ETS2LA.ML.Vision;
using ETS2LA.Game.Telemetry;
using ETS2LA.Shared.Localization;

namespace ETS2LA.Overlay;

#if WINDOWS
    public static class NativeMethods
    {
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod", SetLastError = true)]
        public static extern uint TimeBeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod", SetLastError = true)]
        public static extern uint TimeEndPeriod(uint uMilliseconds);
    }
#endif

public enum FontStyle
{
    Regular,
    Medium,
    SemiBold,
    Bold
}

public class OverlayHandler
{
    private static readonly Lazy<OverlayHandler> _instance = new(() => new OverlayHandler());
    public static OverlayHandler Current => _instance.Value;

    public ControlDefinition Interact = new ControlDefinition
    {   
        Id = "ETS2LA.Overlay.Interact",
        Name = "叠加层交互",
        Description = "按住此按键时，叠加层将接收鼠标输入并允许与其交互。注意：此时无法与叠加层下方的项目交互。",
        DefaultKeybind = "RightAlt",
        Type = ControlType.Boolean
    };

    public ARRenderer AR;
    private OverlaySettings overlaySettings;

    private bool isInteracting = false;
    private float bgOpacityTarget = 0.0f;
    private bool shutdown = false;
    private List<float> frameTimes = new List<float>();
    private List<double> remainingMs = new List<double>();
    
    private List<InternalWindow> windows = new();
    public Dictionary<FontStyle, ImFontPtr> Fonts = new Dictionary<FontStyle, ImFontPtr>();
    public const float DefaultFontSize = 18f;

    public bool IsOverlayFocused => isInteracting;
    public float AverageFrameTime => frameTimes.Count > 0 ? frameTimes.Average() : 0f;
    public float AverageRemainingTime => remainingMs.Count > 0 ? (float)remainingMs.Average() : 0f;

    public float OverlayWidth => GLFW.GetVideoMode(GLFW.GetPrimaryMonitor()).Width;
    public float OverlayHeight => GLFW.GetVideoMode(GLFW.GetPrimaryMonitor()).Height;
    public float OverlayMonitorRefreshRate => GLFW.GetVideoMode(GLFW.GetPrimaryMonitor()).RefreshRate;

    private string glslVersion = "#version 150";
    private GLFWwindowPtr glfwWindow;
    private ImGuiContextPtr imGuiContext;
    private ImPlotContextPtr ImPlotContext;
    private ImGuiIOPtr io;
    private GL gl;

    public OverlayHandler()
    {
        ControlsBackend.Current.RegisterControl(Interact);
        ControlsBackend.Current.On(Interact.Id, HandleInput);
        overlaySettings = OverlaySettingsHandler.Current.GetSettings();
        OverlaySettingsHandler.Current.OnSettingsUpdated += OnOverlaySettingsUpdated;

        Task.Factory.StartNew(RenderLoop, TaskCreationOptions.LongRunning);
        
        windows.Add(new ConsoleWindow());
        windows.Add(new OverlayInfoWindow());
        windows.Add(new DemoWindow());
        windows.Add(new StateWindow());
        windows.Add(new SafetyStatusWindow());

        if (MLSettings.Current.RenderVisionCameras)
            windows.Add(new VisionCamerasWindow());
    }

    private void OnOverlaySettingsUpdated(OverlaySettings newSettings)
    {
        overlaySettings = newSettings;
    }

    private void HandleInput(object sender, ControlChangeEventArgs e)
    {
        bool b = (bool)e.NewValue;
        if (b == isInteracting) { return; }
        isInteracting = b;
    }

    private void RenderLoop()
    {
        if(!InitGLFW())
        {
            Logger.Error("Failed to initialize overlay");
            return;
        }
        GLFW.MakeContextCurrent(glfwWindow);
        gl = new GL(new BindingsContext(glfwWindow));

        if (MLSettings.Current.RenderVisionCameras)
            VisionHandler.Current.Initialize(gl);
        
        if (!InitImGui())
        {
            Logger.Error("Failed to initialize overlay");
            return;
        }

        #if WINDOWS
            // 1ms sleep accuracy on windows
            // Linux is fine from the get go
            NativeMethods.TimeBeginPeriod(1);
        #endif

        Stopwatch fs = Stopwatch.StartNew();
        int targetFramerate = (int)OverlayMonitorRefreshRate;
        double interval = 1000.0 / targetFramerate;
        double next = fs.Elapsed.TotalMilliseconds;
        double start = fs.Elapsed.TotalMilliseconds;

        while (GLFW.WindowShouldClose(glfwWindow) == 0 && !shutdown)
        {
            if (overlaySettings.LimitFramerate)
                interval = 1000.0 / overlaySettings.MaxFramerate;
            else
                interval = 1000.0 / targetFramerate;

            start = fs.Elapsed.TotalMilliseconds;
            if (next < start)
                next = start;
            
            next += interval;

            if (!isInteracting) { 
                // This has to be called each frame to properly update the flags.
                // For whatever reason they are set back to default. Shouldn't affect
                // performance, it's just weird...
                ImGui.GetPlatformIO().Viewports[0].Flags |= ImGuiViewportFlags.NoInputs;
                bgOpacityTarget = 0.0f;

                # if LINUX
                    GLFW.SetWindowAttrib(glfwWindow, GLFW.GLFW_MOUSE_PASSTHROUGH, 1);
                # endif
            }
            else  {
                bgOpacityTarget = 0.5f;
                # if LINUX
                    GLFW.SetWindowAttrib(glfwWindow, GLFW.GLFW_MOUSE_PASSTHROUGH, 0);
                # endif
            }

            GLFW.PollEvents();

            // Skip rendering if we're minimized, though this should actually
            // never happen for the overlay.
            if (GLFW.GetWindowAttrib(glfwWindow, GLFW.GLFW_ICONIFIED) != 0)
            {
                ImGuiImplGLFW.Sleep(10);
                continue;
            }

            GLFW.MakeContextCurrent(glfwWindow);

            ImGuiImplOpenGL3.NewFrame();
            ImGuiImplGLFW.NewFrame();
            ImGui.NewFrame();

            // This renders the virtual cameras into their respective framebuffers.
            if (MLSettings.Current.RenderVisionCameras)
                VisionHandler.Current.Render();

            gl.Viewport(0, 0, (int)OverlayWidth, (int)OverlayHeight);

            // The actual rendering is happening here,
            // all other calls are just setup.
            try { 
                if (AR == null) 
                {
                    AR = new ARRenderer(gl);

                    if (MLSettings.Current.RenderVisionCameras)
                    {
                        VisionCamerasARRenderer renderer = new VisionCamerasARRenderer();
                        renderer.Register();
                    }
                }
                bool paused = GameTelemetry.Current.GetCurrentData().paused;
                if (overlaySettings.RenderAR && (!overlaySettings.DontRenderWhenPaused || !paused)) AR.Render(); 
            }
            catch (Exception ex) {
                Logger.Error($"Error in AR rendering: {ex}");
            }

            try { OnUIRender(); }
            catch (Exception ex) {
                Logger.Error($"Error rendering overlay: {ex}");
            }
            // ---

            ImGui.Render();

            gl.ClearColor(0f, 0f, 0f, bgOpacityTarget);
            gl.Clear(GLClearBufferMask.ColorBufferBit);

            try { AR.RenderShaders(); } 
            catch (Exception ex) {
                Logger.Error($"Error in AR OpenGL pass: {ex}");
            }
            
            ImGuiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());

            if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
            {
                ImGui.UpdatePlatformWindows();
                ImGui.RenderPlatformWindowsDefault();
            }

            GLFW.SwapInterval(0); // disable vsync
            GLFW.SwapBuffers(glfwWindow);

            double remaining = next - fs.Elapsed.TotalMilliseconds;
            if (remaining > 1.0)
                Thread.Sleep((int)(remaining - 1));
            while (fs.Elapsed.TotalMilliseconds < next)
                Thread.SpinWait(10);
            
            frameTimes.Add((float)(fs.Elapsed.TotalMilliseconds - start));
            if (frameTimes.Count > targetFramerate) { frameTimes.RemoveAt(0); }
            
            remainingMs.Add(remaining);
            if (remainingMs.Count > targetFramerate) { remainingMs.RemoveAt(0); }
        }

        ImGuiImplOpenGL3.Shutdown();
        ImGuiImplOpenGL3.SetCurrentContext(null);
        ImGuiImplGLFW.Shutdown();
        ImGuiImplGLFW.SetCurrentContext(null);
        ImGui.DestroyContext();
        gl.Dispose();

        // Clean up and terminate GLFW
        GLFW.DestroyWindow(glfwWindow);
        GLFW.Terminate();
    }

    private void OnUIRender()
    {
        if (isInteracting)
        {
            ImGui.Begin(AppLocalization.Translate("交互模式"), ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoBackground);
            ImGui.SetWindowPos(new Vector2(OverlayWidth / 2 - 60, 10), ImGuiCond.Always);
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), AppLocalization.Translate("交互模式"));

            ImGui.Spacing();
            try
            {
                foreach (var window in windows) {
                    bool isOpen = window.IsWindowOpen;
                    Vector4 color = isOpen ? new Vector4(0.5f, 0.6f, 0.5f, 1f) : new Vector4(0.6f, 0.5f, 0.5f, 1f);

                    ImGui.TextColored(color, isOpen ? "[X]" : "[   ]");
                    ImGui.SameLine();
                    ImGui.TextColored(color, AppLocalization.Translate(window.Definition.Title));
        
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(AppLocalization.Translate("点击") + (isOpen ? AppLocalization.Translate("隐藏") : AppLocalization.Translate("显示")) + AppLocalization.Translate("此窗口"));
                    }
                    if (ImGui.IsItemClicked())
                    {
                        window.IsWindowOpen = !window.IsWindowOpen;
                    }
                }
            } catch (Exception ex)
            {
                Logger.Error($"Error rendering interaction mode window controls: {ex}");
            }
            ImGui.End();
        }

        ImGui.SetNextWindowPos(new Vector2(OverlayWidth - 10, 10), ImGuiCond.Always, new Vector2(1f, 0f));
        ImGui.Begin(AppLocalization.Translate("性能叠加层"), ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoBackground);

        var fps = (AverageFrameTime > 0) ? (int)(1 / (AverageFrameTime / 1000f)) : 0;
        int freePercentage;
        if (overlaySettings.LimitFramerate)
            freePercentage = (int)(AverageRemainingTime / (1000f / overlaySettings.MaxFramerate) * 100f);
        else
            freePercentage = (int)(AverageRemainingTime / (1000f / OverlayMonitorRefreshRate) * 100f);
        
        ImGui.TextColored(new Vector4(1f,1f,1f,0.5f), $"{(int)(1/(AverageFrameTime / 1000f))}");
        ImGui.TextColored(new Vector4(1f,1f,1f,0.5f), $"{freePercentage}%%");
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(AppLocalization.Translate("叠加层空闲 CPU 时间百分比"));
        }

        ImGui.End();

        foreach (InternalWindow window in windows)
        {
            try
            {
                if (!window.IsWindowOpen) { continue; }
                if (window.Definition.NoWindow.GetValueOrDefault(false))
                {
                    window.Render();
                    continue;
                }

                // Plugin developer has set the width and height, these get applied
                // once each startup. Use ImGuiWindowFlags to disallow movement.
                if (window.Definition.Width.HasValue || window.Definition.Height.HasValue)
                {
                    var width = window.Definition.Width.GetValueOrDefault(480);
                    var height = window.Definition.Height.GetValueOrDefault(320);
                    if (width > 0 && height > 0)
                        ImGui.SetNextWindowSize(new Vector2(width, height), ImGuiCond.Once);
                }

                // Plugin developer has set a sizing function, this is continuous
                // throughout the lifecycle of the window.
                if (window.Definition.SizingFunction.HasValue && window.Definition.SizingFunction.Value != null)
                {
                    var (width, height) = window.Definition.SizingFunction.Value();
                    ImGui.SetNextWindowSize(new Vector2(width, height), ImGuiCond.Always);
                }

                ImGui.SetNextWindowBgAlpha(window.Definition.Alpha.GetValueOrDefault(0.9f));
                ImGui.Begin(AppLocalization.Translate(window.Definition.Title), window.Definition.Flags.GetValueOrDefault(ImGuiWindowFlags.None));
                
                // Plugin developer has set X and Y values, these are applied
                // once each startup. Use ImGuiWindowFlags to disallow resizing.
                if (window.Definition.X.HasValue || window.Definition.Y.HasValue)
                {
                    int x = (int)window.Definition.X.GetValueOrDefault(OverlayWidth / 2);
                    int y = (int)window.Definition.Y.GetValueOrDefault(OverlayHeight / 2);
                    if (x >= 0 && y >= 0)
                    {
                        ImGui.SetWindowPos(new Vector2(x, y), ImGuiCond.Once);
                    }
                } 
                // If no X and Y are set, then for the first time we'll open this window
                // at the center of the screen.
                else
                {
                    int x = (int)OverlayWidth / 2;
                    int y = (int)OverlayHeight / 2;
                    if (x >= 0 && y >= 0)
                    {
                        ImGui.SetWindowPos(new Vector2(x, y), ImGuiCond.FirstUseEver);
                    }
                }

                // Plugin developer has set a location function, this is continuous
                // throughout the lifecycle of the window.
                if (window.Definition.LocationFunction.HasValue && window.Definition.LocationFunction.Value != null)
                {
                    var (x, y) = window.Definition.LocationFunction.Value();
                    ImGui.SetWindowPos(new Vector2(x, y), ImGuiCond.Always);
                }

                var isCollapsed = ImGui.IsWindowCollapsed();
                if (isCollapsed) {
                    ImGui.End(); 
                    continue; 
                }

                try
                {
                    RenderWindowContextMenu(window);
                } catch (Exception ex)
                {
                    Logger.Error($"Error rendering context menu for window {window.Definition.Title}: {ex}");
                }

                try
                {
                    window.Render();
                } catch (Exception ex)
                {
                    Logger.Error($"Error rendering window {window.Definition.Title}: {ex}");
                }

                ImGui.End();
            }
            catch (Exception ex)
            {
                try { ImGui.End(); }
                catch { }
                Logger.Error($"Error rendering window {window.Definition.Title}: {ex}");
            }
        }
    }

    private unsafe void RenderWindowContextMenu(InternalWindow window)
    {
        if (ImGui.BeginPopupContextWindow((byte*)0, ImGuiPopupFlags.MouseButtonRight))
        {
            window.RenderContextMenu();
            if (ImGui.MenuItem("关闭"))
            {
                window.IsWindowOpen = false;
            }
            ImGui.EndPopup();
        }
    }

    private bool InitImGui()
    {
        imGuiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(imGuiContext);
        
        ImPlot.SetImGuiContext(imGuiContext);
        ImPlotContext = ImPlot.CreateContext();
        ImPlot.SetCurrentContext(ImPlotContext);

        io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;     // Enable Keyboard Controls
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;      // Enable Gamepad Controls
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;         // Enable Docking
        if (OverlaySettingsHandler.Current.GetSettings().SupportMultipleViewports)
            io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;       // Enable Multi-Viewport / Platform Windows

        // Tweak our styling just a little
        ImGui.StyleColorsDark();
        var style = ImGui.GetStyle();
        style.WindowRounding = 3.0f;
        style.Colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.1f, 0.1f, 0.1f, 1f);

        // And then we scale by the monitor DPI
        var mon = GLFW.GetPrimaryMonitor();
        float mainScale = ImGuiImplGLFW.GetContentScaleForMonitor(Unsafe.BitCast<Hexa.NET.GLFW.GLFWmonitorPtr, Hexa.NET.ImGui.Backends.GLFW.GLFWmonitorPtr>(mon));

        style.ScaleAllSizes(mainScale);
        style.FontScaleDpi = mainScale;
        io.ConfigDpiScaleFonts = true;
        io.ConfigDpiScaleViewports = true;

        List<Tuple<FontStyle, string>> fonts = new List<Tuple<FontStyle, string>>()
        {
            new Tuple<FontStyle, string>(FontStyle.Medium, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Fonts", "Geist-Medium.ttf")),
            new Tuple<FontStyle, string>(FontStyle.Regular, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Fonts", "Geist-Regular.ttf")),
            new Tuple<FontStyle, string>(FontStyle.SemiBold, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Fonts", "Geist-SemiBold.ttf")),
            new Tuple<FontStyle, string>(FontStyle.Bold, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Fonts", "Geist-Bold.ttf")),
        };

        unsafe
        {
            string[] cjkFontCandidates =
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Fonts", "NotoSansSC-VF.ttf"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "NotoSansSC-VF.ttf"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "simhei.ttf"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "simsun.ttc")
            };
            string? cjkFontPath = cjkFontCandidates.FirstOrDefault(File.Exists);
            ImFont* cjkFont = null;
            if (cjkFontPath is not null)
            {
                uint[] cjkRanges =
                {
                    0x0020, 0x00FF, 0x2000, 0x206F, 0x3000, 0x303F,
                    0x3040, 0x30FF, 0x3400, 0x4DBF, 0x4E00, 0x9FFF,
                    0xFF00, 0xFFEF, 0
                };
                fixed (uint* ranges = cjkRanges)
                    cjkFont = io.Fonts.AddFontFromFileTTF(cjkFontPath, DefaultFontSize, ranges);
            }

            if (cjkFont is not null)
            {
                // ImGui does not automatically fall back to a separately loaded font.
                // Use the CJK font for every style so Chinese text never becomes '?'.
                var cjkFontPtr = new ImFontPtr(cjkFont);
                foreach (FontStyle fontStyle in Enum.GetValues<FontStyle>())
                    Fonts[fontStyle] = cjkFontPtr;
                Logger.Info($"Loaded CJK overlay font: {cjkFontPath}");
            }
            else
            {
                foreach (var font in fonts)
                {
                    if (!File.Exists(font.Item2))
                    {
                        Logger.Error($"Font file not found at {font.Item2}");
                        continue;
                    }
                    ImFont* nativeFont = io.Fonts.AddFontFromFileTTF(font.Item2, DefaultFontSize);
                    Fonts[font.Item1] = new ImFontPtr(nativeFont);
                }
                Logger.Warn("No CJK font found; Chinese overlay text may render as question marks.");
            }
        }

        ImGuiImplGLFW.SetCurrentContext(imGuiContext);
        if (!ImGuiImplGLFW.InitForOpenGL(Unsafe.BitCast<GLFWwindowPtr, Hexa.NET.ImGui.Backends.GLFW.GLFWwindowPtr>(glfwWindow), true))
        {
            Logger.Error("Failed to init ImGui Impl GLFW");
            GLFW.Terminate();
            return false;
        }

        ImGuiImplOpenGL3.SetCurrentContext(imGuiContext);
        if (!ImGuiImplOpenGL3.Init(glslVersion))
        {
            Logger.Error("Failed to init ImGui Impl OpenGL3");
            GLFW.Terminate();
            return false;
        }

        gl.Enable(GLEnableCap.Blend);
        gl.BlendFunc(GLBlendingFactor.SrcAlpha, GLBlendingFactor.OneMinusSrcAlpha);
        gl.ClearColor(0f, 0f, 0f, 0f); // Transparent background
        return true;
    }

    private bool InitGLFW()
    {
        unsafe
        {
            GLFW.SetErrorCallback((error, description) =>
            {
                # if DEBUG
                Logger.Error($"GLFW Error {error}: {Utils.DecodeStringUTF8(description)}");
                # endif
            });
        }

        unsafe
        {
            Logger.Info("Initializing GLFW Version: " + Utils.DecodeStringUTF8(GLFW.GetVersionString()));
        }

        // This code sets the platform to X11 instead of wayland. This only needs to be
        // done inside vscode for whatever reason. https://github.com/opentk/opentk/issues/1823
        string? sessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");
        string? useWayland = Environment.GetEnvironmentVariable("GLFW_USE_WAYLAND");
        if (sessionType == "wayland" && useWayland == "0")
        {
            GLFW.InitHint(GLFW.GLFW_PLATFORM, GLFW.GLFW_PLATFORM_X11);
        }

        Console.WriteLine("Initializing GLFW...");
        GLFW.Init();
        GLFW.WindowHint(GLFW.GLFW_CONTEXT_VERSION_MAJOR, 3);
        GLFW.WindowHint(GLFW.GLFW_CONTEXT_VERSION_MINOR, 2);
        GLFW.WindowHint(GLFW.GLFW_OPENGL_PROFILE, GLFW.GLFW_OPENGL_CORE_PROFILE);  // 3.2+ only

        GLFW.WindowHint(GLFW.GLFW_TRANSPARENT_FRAMEBUFFER, 1);  // Transparent
        GLFW.WindowHint(GLFW.GLFW_DECORATED, 0);                // No window decorations
        GLFW.WindowHint(GLFW.GLFW_FLOATING, 1);                 // Always on top
        GLFW.WindowHint(GLFW.GLFW_FOCUSED, 0);                  // Start unfocused
        GLFW.WindowHint(GLFW.GLFW_FOCUS_ON_SHOW, 0);            // Start unfocused
        
        var mon = GLFW.GetPrimaryMonitor();
        int width, height;
        width = GLFW.GetVideoMode(mon).Width;
        height = GLFW.GetVideoMode(mon).Height;

        // NOTE: Width and height set to screen-2
        // If they are set to the screen size, windows does some optimizations that cause the window
        // to go full black when focused. Setting these to -2 seems to prevent that.
        glfwWindow = GLFW.CreateWindow(width - 2, height - 2, "ETS2LA overlay", null, null);
        if (glfwWindow.IsNull)
        {
            Logger.Error("Failed to create GLFW window");
            GLFW.Terminate();
            return false;
        }

        # if WINDOWS
        GLFW.SetWindowPos(glfwWindow, 1, 1);
        # endif

        return true;
    }

    public void RegisterWindow(WindowDefinition def, Action renderAction, Optional<Action> renderContextMenuAction = default)
    {
        foreach (var window in windows)
        {
            if (window.Definition.Title == def.Title)
            {
                window.Definition = def;
                window.Render = renderAction;
                window.RenderContextMenu = renderContextMenuAction.GetValueOrDefault(() => { });
                return;
            }
        }
        
        var newWindow = new ExternalWindow(def, renderAction, renderContextMenuAction.GetValueOrDefault(() => { }));
        if (def.Open.HasValue)
        {
            newWindow.IsWindowOpen = def.Open.Value;
        }

        windows.Add(newWindow);
    }

    public void UnregisterWindow(WindowDefinition def)
    {
        windows.RemoveAll(w => w.Definition.Title == def.Title);
    }

    public void OpenWindow(string windowName)
    {
        var window = windows.FirstOrDefault(w => w.Definition.Title == windowName);
        if (window != null)
        {
            window.IsWindowOpen = true;
        }
    }

    public void CloseWindow(string windowName)
    {
        var window = windows.FirstOrDefault(w => w.Definition.Title == windowName);
        if (window != null)
        {
            window.IsWindowOpen = false;
        }
    }

    public void Shutdown()
    {
        shutdown = true;
        VisionHandler.Current.Shutdown();
    }
}
