using ETS2LA.Game.Output;

namespace ETS2LA.Hardened.Tests;

public sealed class SpeedLimitGuardTests
{
    [Fact]
    public void LimitAcceleration_AllowsBelowLimit()
    {
        var result = SpeedLimitGuard.LimitAcceleration(speedMps: 20f, maxSpeedMps: 30f, acceleration: 0.5f);
        Assert.Equal(0.5f, result, precision: 4);
    }

    [Fact]
    public void LimitAcceleration_CutsPositiveThrottleAboveLimit()
    {
        var result = SpeedLimitGuard.LimitAcceleration(speedMps: 31f, maxSpeedMps: 30f, acceleration: 0.5f);
        Assert.True(result <= 0f);
    }

    [Fact]
    public void LimitAcceleration_KeepsUserBrakingAboveLimit()
    {
        var result = SpeedLimitGuard.LimitAcceleration(speedMps: 35f, maxSpeedMps: 30f, acceleration: -0.8f);
        Assert.Equal(-0.8f, result, precision: 4);
    }

    [Fact]
    public void LimitAcceleration_AppliesBrakingOnStrongOverspeed()
    {
        var result = SpeedLimitGuard.LimitAcceleration(speedMps: 40f, maxSpeedMps: 30f, acceleration: 0f);
        Assert.True(result < -0.1f);
    }

    [Fact]
    public void LimitAcceleration_DoesNotExceedMaxBrake()
    {
        var result = SpeedLimitGuard.LimitAcceleration(speedMps: 300f, maxSpeedMps: 30f, acceleration: 0f);
        Assert.True(result >= -SpeedLimitGuard.MaxBrake - 1e-4f);
        Assert.True(result <= 0f);
    }

    [Fact]
    public void LimitAcceleration_DisabledWhenMaxSpeedUnset()
    {
        var result = SpeedLimitGuard.LimitAcceleration(speedMps: 50f, maxSpeedMps: 0f, acceleration: 0.5f);
        Assert.Equal(0.5f, result, precision: 4);
    }
}