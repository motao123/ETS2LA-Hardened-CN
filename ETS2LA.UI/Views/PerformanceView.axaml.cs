using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using ETS2LA.Overlay;
using ETS2LA.Game.Telemetry;
using ETS2LA.Shared;
using ETS2LA.UI.Localization;

namespace ETS2LA.UI.Views;

public partial class PerformanceView : UserControl
{
    private readonly DispatcherTimer _timer = new();
    private readonly SampleHistory fpsHistory = new(60);
    private readonly Rectangle[] bars = new Rectangle[60];
    private TimeSpan _lastCpuTime;
    private DateTime _lastCpuSample;

    public PerformanceView()
    {
        InitializeComponent();
        DataContext = this;
        LocalizationManager.Localize(this);

        for (var i = 0; i < bars.Length; i++)
        {
            bars[i] = new Rectangle { Width = 7, Fill = new SolidColorBrush(Color.Parse("#00d4a4")) };
            Canvas.SetLeft(bars[i], i * 9d);
            ((Canvas)this.FindControl<Canvas>("FpsCanvas")!).Children.Add(bars[i]);
        }

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

        fpsHistory.Add(fps);
        RenderTrend();

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

    private void RenderTrend()
    {
        const double chartHeight = 72d;
        var samples = fpsHistory.ToArray();
        var max = Math.Max(fpsHistory.Max(), 1f);

        for (var i = 0; i < bars.Length; i++)
        {
            if (i < samples.Length)
            {
                var h = samples[i] / max * chartHeight;
                bars[i].Height = Math.Clamp(h, 1d, chartHeight);
                Canvas.SetTop(bars[i], 80d - bars[i].Height - 4d);
                bars[i].Opacity = 1d;
            }
            else
            {
                bars[i].Height = 0d;
                bars[i].Opacity = 0.15d;
            }
        }

        Set("FpsStatsValue", string.Format(LocalizationManager.TranslateLiteral("最近：最低 {0} / 平均 {1} / 最高 {2} FPS"),
            fpsHistory.Min().ToString("F1"), fpsHistory.Average().ToString("F1"), fpsHistory.Max().ToString("F1")));
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