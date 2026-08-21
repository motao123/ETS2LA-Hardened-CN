namespace ETS2LA.Game.Output;

/// <summary>
/// 主机侧失效安全：限制单个控制量每个 tick 的变化幅度（slew-rate 限幅），
/// 防止急转/急加油引起失控抖动；同时提供遥测陈旧检测。
/// </summary>
public static class OutputSafety
{
    /// <summary>默认每 tick 最大变化量。约 60Hz 下全量程需 0.1s 左右。 </summary>
    public const float DefaultMaxDelta = 0.2f;

    public static float LimitSlew(float previous, float target, float maxDelta = DefaultMaxDelta)
    {
        if (!float.IsFinite(target)) return previous;
        if (!float.IsFinite(previous)) return Math.Clamp(target, -1f, 1f);
        if (maxDelta <= 0f) return Math.Clamp(previous, -1f, 1f);

        var clamped = Math.Clamp(target, -1f, 1f);
        var delta = Math.Clamp(clamped - previous, -maxDelta, maxDelta);
        return Math.Clamp(previous + delta, -1f, 1f);
    }

    public static bool IsStale(DateTimeOffset lastUpdate, TimeSpan maxAge)
        => DateTimeOffset.UtcNow - lastUpdate > maxAge;
}