using System.Diagnostics;
using ETS2LA.Game.Telemetry;

namespace ETS2LA.Hardened.Tests;

public sealed class TelemetryFreshnessTests
{
    [Fact]
    public void IsFresh_ReturnsTrueWithinAgeLimit()
    {
        var current = Stopwatch.Frequency * 10L;
        var last = current - Stopwatch.Frequency;

        Assert.True(TelemetryFreshness.IsFresh(last, current, sdkActive: true, TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public void IsFresh_ReturnsFalseWhenStale()
    {
        var current = Stopwatch.Frequency * 10L;
        var last = current - Stopwatch.Frequency * 3L;

        Assert.False(TelemetryFreshness.IsFresh(last, current, sdkActive: true, TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public void IsFresh_ReturnsFalseWhenSdkInactive()
    {
        Assert.False(TelemetryFreshness.IsFresh(1, 1, sdkActive: false, TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public void IsFresh_ReturnsFalseForMissingTimestamp()
    {
        Assert.False(TelemetryFreshness.IsFresh(0, Stopwatch.Frequency, sdkActive: true, TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public void IsFresh_ReturnsFalseWhenClockMovesBackward()
    {
        Assert.False(TelemetryFreshness.IsFresh(Stopwatch.Frequency * 2L, Stopwatch.Frequency, sdkActive: true, TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public void IsFresh_ReturnsFalseForInvalidAgeLimit()
    {
        Assert.False(TelemetryFreshness.IsFresh(1, 1, sdkActive: true, TimeSpan.Zero));
    }
}