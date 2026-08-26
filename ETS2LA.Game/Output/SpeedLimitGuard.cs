namespace ETS2LA.Game.Output;

/// <summary>
/// 主机侧超速保护：当车辆实际速度超过配置上限时，禁止正油门；
/// 明显超速时按超过程度施加渐进制动。
/// </summary>
public static class SpeedLimitGuard
{
    /// <summary>进入“施加制动”的超速下限（m/s），约 3.6 km/h。</summary>
    public const float BrakeOverspeedStart = 1.0f;

    /// <summary>超速保护施加的最大制动值（-1 为全力刹车）。</summary>
    public const float MaxBrake = 0.6f;

    public static float LimitAcceleration(float speedMps, float maxSpeedMps, float acceleration)
    {
        if (maxSpeedMps <= 0f || !float.IsFinite(speedMps) || !float.IsFinite(acceleration))
            return acceleration;

        if (speedMps <= maxSpeedMps)
            return acceleration;

        var overspeed = speedMps - maxSpeedMps;
        // 超过上限：一律不允许正油门
        var result = Math.Min(acceleration, 0f);

        // 明显超速：按超过程度施加渐进制动
        if (overspeed >= BrakeOverspeedStart)
        {
            var brake = Math.Min(MaxBrake, 0.1f + overspeed * 0.05f);
            result = Math.Min(result, -brake);
        }

        return Math.Clamp(result, -1f, 0f);
    }
}