using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ETS2LA.Overlay;
using ETS2LA.Game.Telemetry;
using ETS2LA.UI.Localization;

namespace ETS2LA.UI.Views;

public partial class PerformanceView : UserControl
{
    private readonly DispatcherTimer _timer = new();
    private TimeSpan _lastCpuTime;
    private DateTime _lastCpuSample;

    public PerformanceView()
    {
        InitializeComponent();
        DataContext = this;
        LocalizationManager.Localize(this);

        _lastCpuTime = Process.GetCurrentProcess().TotalProcessorTime;
        _lastCpuSample = DateTime.UtcNow;
        _timer.Interval = TimeSpan.FromMilliseconds(500);
        _timer.Tick += (_, _) => Refresh();
        _timer.Start();
        Refresh();
    }

    private void Refresh()
    {
        var overlay = OverlayHandler.Current;
        var frameMs = overlay.AverageFrameTime;
        var fps = frameMs > 0 ? 1000f / frameMs : 0f;

        Set("FpsValue", fps.ToString("F1"));
        Set("FrameTimeValue", frameMs.ToString("F2"));
        Set("RemainingValue", overlay.AverageRemainingTime.ToString("F2"));

        var process = Process.GetCurrentProcess();
        var now = DateTime.UtcNow;
        var cpuTime = process.TotalProcessorTime;
        var cpuPercent = 0d;
        var elapsed = (now - _lastCpuSample).TotalMilliseconds;
        if (elapsed > 0)
        {
            cpuPercent = (cpuTime - _lastCpuTime).TotalMilliseconds / elapsed * 100d;
        }
        _lastCpuTime = cpuTime;
        _lastCpuSample = now;
        Set("CpuValue", cpuPercent.ToString("F1") + "%");

        Set("WorkingSetValue", (process.WorkingSet64 / 1024d / 1024d).ToString("F0"));
        Set("GcMemoryValue", (GC.GetTotalMemory(false) / 1024d / 1024d).ToString("F0"));

        var telemetry = GameTelemetry.Current.GetCurrentData();
        if (telemetry != null && telemetry.sdkActive)
        {
            Set("GameStatusValue", LocalizationManager.TranslateLiteral("已连接"));
            Set("PausedValue", telemetry.paused ? LocalizationManager.TranslateLiteral("已暂停") : LocalizationManager.TranslateLiteral("运行中"));
        }
        else
        {
            Set("GameStatusValue", LocalizationManager.TranslateLiteral("未连接"));
            Set("PausedValue", "-");
        }
    }

    private void Set(string name, string value)
    {
        if (this.FindControl<TextBlock>(name) is TextBlock block)
            block.Text = value;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}