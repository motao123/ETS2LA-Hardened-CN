using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ETS2LA.Backend;
using ETS2LA.Game.Output;
using ETS2LA.Game.Telemetry;
using ETS2LA.Settings.Global;
using ETS2LA.Shared;
using ETS2LA.State;
using ETS2LA.UI.Localization;

namespace ETS2LA.UI.Views;

public partial class VisualizationView : UserControl
{
    private readonly DispatcherTimer _timer = new();

    public VisualizationView()
    {
        InitializeComponent();
        DataContext = this;
        LocalizationManager.Localize(this);

        _timer.Interval = TimeSpan.FromMilliseconds(250);
        _timer.Tick += (_, _) => Refresh();
        _timer.Start();
        Refresh();
    }

    private void Refresh()
    {
        RefreshAssistState();
        RefreshPlugins();
        RefreshSafetyState();

        var data = GameTelemetry.Current.GetCurrentData();
        if (data == null || !data.sdkActive)
        {
            Set("StatusValue", LocalizationManager.TranslateLiteral("未连接"));
            Set("SpeedValue", "-");
            Set("RpmValue", "-");
            Set("GearValue", "-");
            Set("CruiseValue", "-");
            Set("ControlValue", "-");
            Set("FuelValue", "-");
            Set("BlinkerValue", "-");
            Set("VehicleValue", "-");
            Set("TruckValue", "-");
            Set("CargoValue", "-");
            Set("SourceValue", "-");
            Set("DestValue", "-");
            Set("CoordXValue", "-");
            Set("CoordYValue", "-");
            Set("CoordZValue", "-");
            Set("CruiseActiveValue", "-");
            return;
        }

        Set("StatusValue", LocalizationManager.TranslateLiteral("已连接"));

        var f = data.truckFloat;
        Set("SpeedValue", (f.speed * 3.6f).ToString("F1"));
        Set("RpmValue", f.engineRpm.ToString("F0"));
        Set("GearValue", data.truckInt.gear.ToString());

        Set("CruiseValue", f.cruiseControlSpeed > 0 ? (f.cruiseControlSpeed * 3.6f).ToString("F1") : "-");
        Set("ControlValue", string.Format(LocalizationManager.TranslateLiteral("油门 {0} / 刹车 {1} / 转向 {2}"),
            f.userThrottle.ToString("F2"), f.userBrake.ToString("F2"), f.userSteer.ToString("F2")));

        var capacity = data.configFloat.fuelCapacity > 0 ? data.configFloat.fuelCapacity : 1f;
        Set("FuelValue", $"{f.fuel:F0} L ({f.fuel / capacity * 100f:F0}%)");

        var b = data.truckBool;
        string blinker = b.blinkerLeftOn && b.blinkerRightOn ? LocalizationManager.TranslateLiteral("双闪")
            : b.blinkerLeftOn ? LocalizationManager.TranslateLiteral("左转")
            : b.blinkerRightOn ? LocalizationManager.TranslateLiteral("右转")
            : LocalizationManager.TranslateLiteral("关");
        Set("BlinkerValue", blinker);

        Set("VehicleValue",
            (b.parkingBrake ? LocalizationManager.TranslateLiteral("驻车制动") : LocalizationManager.TranslateLiteral("行车")) +
            " / " +
            (b.engineEnabled ? LocalizationManager.TranslateLiteral("发动机启动") : LocalizationManager.TranslateLiteral("发动机未启动")));

        var cs = data.configString;
        Set("TruckValue", string.IsNullOrWhiteSpace(cs.truckBrand) ? "-" : $"{cs.truckBrand} {cs.truckName}".Trim());
        Set("CargoValue", string.IsNullOrWhiteSpace(cs.cargo) ? "-" : cs.cargo);
        Set("SourceValue", string.IsNullOrWhiteSpace(cs.citySrc) ? "-" : cs.citySrc);
        Set("DestValue", string.IsNullOrWhiteSpace(cs.cityDst) ? "-" : cs.cityDst);

        var pos = data.truckPlacement.coordinate;
        Set("CoordXValue", pos.X.ToString("F1"));
        Set("CoordYValue", pos.Y.ToString("F1"));
        Set("CoordZValue", pos.Z.ToString("F1"));

        Set("CruiseActiveValue", b.cruiseControl
            ? LocalizationManager.TranslateLiteral("已激活")
            : LocalizationManager.TranslateLiteral("未激活"));
    }

    private void RefreshAssistState()
    {
        var state = ApplicationState.Current;
        Set("SteerAssistValue", SteeringText(state.DesiredSteeringLevel));
        Set("LongAssistValue", LongitudinalText(state.DesiredLongitudinalLevel));
        Set("SteerPausedValue", state.PauseSteeringAssist
            ? LocalizationManager.TranslateLiteral("是")
            : LocalizationManager.TranslateLiteral("否"));
        Set("LongPausedValue", state.PauseLongitudinalAssist
            ? LocalizationManager.TranslateLiteral("是")
            : LocalizationManager.TranslateLiteral("否"));
        Set("DesiredSpeedValue", state.DesiredSpeed > 0 ? (state.DesiredSpeed * 3.6f).ToString("F1") : "-");
    }

    private void RefreshSafetyState()
    {
        var output = GameOutput.Current.LastOutput;
        Set("OutputSteeringValue", output.Steering.ToString("F2"));
        Set("OutputAccelValue", output.Acceleration.ToString("F2"));
        Set("OutputStatusValue", output.IsStale(TimeSpan.FromSeconds(1))
            ? LocalizationManager.TranslateLiteral("停止输出")
            : LocalizationManager.TranslateLiteral("正在写入"));

        var fresh = GameTelemetry.Current.IsFresh(TimeSpan.FromSeconds(2));
        Set("FreshnessValue", fresh
            ? LocalizationManager.TranslateLiteral("新鲜")
            : LocalizationManager.TranslateLiteral("陈旧（已复位输出）"));

        var maxMps = AssistanceSettings.Current.MaximumSpeed;
        var unit = ToSpeedUnit(StateSettingsHandler.Current.GetSettings().DisplayUnits);
        Set("MaxSpeedValue", maxMps > 0
            ? $"{SpeedUnitConverter.FromMetersPerSecond(maxMps, unit):F0} {GetSpeedAbbreviation(unit)}"
            : LocalizationManager.TranslateLiteral("不限速"));

        if (maxMps <= 0f)
        {
            Set("OverspeedValue", "-");
            Set("SpeedGuardValue", LocalizationManager.TranslateLiteral("未生效（未设限速）"));
            return;
        }

        var speedMps = GameTelemetry.Current.GetCurrentData()?.truckFloat.speed ?? 0f;
        var deltaMps = speedMps - maxMps;
        if (deltaMps <= 0f)
        {
            Set("OverspeedValue", LocalizationManager.TranslateLiteral("未超速"));
            Set("SpeedGuardValue", LocalizationManager.TranslateLiteral("未生效"));
            return;
        }

        Set("OverspeedValue", string.Format(LocalizationManager.TranslateLiteral("超速 +{0} {1}"),
            SpeedUnitConverter.FromMetersPerSecond(deltaMps, unit).ToString("F1"), GetSpeedAbbreviation(unit)));

        var guarded = SpeedLimitGuard.LimitAcceleration(speedMps, maxMps, acceleration: 1f);
        Set("SpeedGuardValue", guarded < 0f
            ? LocalizationManager.TranslateLiteral("正在制动")
            : guarded < 1f
                ? LocalizationManager.TranslateLiteral("禁止油门")
                : LocalizationManager.TranslateLiteral("未生效"));
    }

    private static ETS2LA.Shared.SpeedUnit ToSpeedUnit(ETS2LA.State.Units units) => units switch
    {
        ETS2LA.State.Units.Imperial => ETS2LA.Shared.SpeedUnit.Imperial,
        ETS2LA.State.Units.Scientific => ETS2LA.Shared.SpeedUnit.Scientific,
        _ => ETS2LA.Shared.SpeedUnit.Metric
    };

    private static string GetSpeedAbbreviation(SpeedUnit unit) => unit switch
    {
        SpeedUnit.Imperial => "mph",
        SpeedUnit.Scientific => "m/s",
        _ => "km/h"
    };

    private void RefreshPlugins()
    {
        var handler = PluginBackend.Current?.PluginHandler;
        if (handler == null || handler.LoadedPlugins.Count == 0)
        {
            Set("PluginsValue", "-");
            return;
        }

        var names = string.Join("、", handler.LoadedPlugins.Select(p => p.Info.Id.Split('.').Last()));
        Set("PluginsValue", names);
    }

    private static string SteeringText(SteeringAssists value) => value switch
    {
        SteeringAssists.None => LocalizationManager.TranslateLiteral("无"),
        SteeringAssists.LaneKeep => LocalizationManager.TranslateLiteral("车道保持"),
        SteeringAssists.Full => LocalizationManager.TranslateLiteral("完整"),
        _ => value.ToString()
    };

    private static string LongitudinalText(LongitudinalAssists value) => value switch
    {
        LongitudinalAssists.None => LocalizationManager.TranslateLiteral("无"),
        LongitudinalAssists.EmergencyBraking => LocalizationManager.TranslateLiteral("紧急制动"),
        LongitudinalAssists.AdaptiveCruiseControl => LocalizationManager.TranslateLiteral("自适应巡航"),
        _ => value.ToString()
    };

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