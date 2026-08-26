using System.Diagnostics;

namespace ETS2LA.Game.Telemetry;

public static class TelemetryFreshness
{
    public static bool IsFresh(
        long lastUpdateTimestamp,
        long currentTimestamp,
        bool sdkActive,
        TimeSpan maxAge)
    {
        if (!sdkActive || lastUpdateTimestamp <= 0 ||
            currentTimestamp < lastUpdateTimestamp || maxAge <= TimeSpan.Zero)
            return false;

        var elapsedTicks = currentTimestamp - lastUpdateTimestamp;
        var elapsedSeconds = elapsedTicks / (double)Stopwatch.Frequency;
        return elapsedSeconds <= maxAge.TotalSeconds;
    }
}