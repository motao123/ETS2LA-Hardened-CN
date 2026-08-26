namespace ETS2LA.Shared;

/// <summary>
/// 速度单位换算（仅速度）。主机与设置统一以 m/s（科学单位）存储/计算，
/// 只在 UI 展示层换算为 km/h / mph。
/// </summary>
public enum SpeedUnit
{
    Metric,
    Imperial,
    Scientific
}

public static class SpeedUnitConverter
{
    /// <summary>把显示单位的速度转成 m/s（科学单位）。</summary>
    public static float ToMetersPerSecond(float value, SpeedUnit unit) => unit switch
    {
        SpeedUnit.Metric => value / 3.6f,       // km/h -> m/s
        SpeedUnit.Imperial => value * 0.44704f, // mph -> m/s
        SpeedUnit.Scientific => value,          // already m/s
        _ => value
    };

    /// <summary>把 m/s 转成显示单位的速度。</summary>
    public static float FromMetersPerSecond(float value, SpeedUnit unit) => unit switch
    {
        SpeedUnit.Metric => value * 3.6f,       // m/s -> km/h
        SpeedUnit.Imperial => value / 0.44704f, // m/s -> mph
        SpeedUnit.Scientific => value,          // already m/s
        _ => value
    };
}