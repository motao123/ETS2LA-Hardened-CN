using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ETS2LA.Game.Telemetry;
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