using System.Diagnostics;

namespace ETS2LA.Game.Output;

/// <summary>
/// 记录主机最后一次写出的转向/油门（含刹车）控制值，用于在界面上展示
/// “主机当前是否在向游戏输出控制”。
/// </summary>
public sealed class OutputSnapshot
{
    public float Steering { get; private set; }
    public float Acceleration { get; private set; }
    public long TimestampTicks { get; private set; }

    public bool HasData => TimestampTicks > 0;

    public void Record(float steering, float acceleration)
    {
        Steering = Math.Clamp(steering, -1f, 1f);
        Acceleration = Math.Clamp(acceleration, -1f, 1f);
        TimestampTicks = Stopwatch.GetTimestamp();
    }

    public bool IsStale(TimeSpan maxAge)
    {
        if (!HasData || maxAge <= TimeSpan.Zero)
            return !HasData;
        var elapsed = (Stopwatch.GetTimestamp() - TimestampTicks) / (double)Stopwatch.Frequency;
        return elapsed > maxAge.TotalSeconds;
    }
}