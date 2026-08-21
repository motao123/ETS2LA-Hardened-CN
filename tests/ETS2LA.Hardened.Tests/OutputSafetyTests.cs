using ETS2LA.Game.Output;

namespace ETS2LA.Hardened.Tests;

public sealed class OutputSafetyTests
{
    [Fact]
    public void LimitSlew_ClampsChangeToMaxDelta()
    {
        var limited = OutputSafety.LimitSlew(0f, 1f, maxDelta: 0.25f);
        Assert.Equal(0.25f, limited, precision: 4);
    }

    [Fact]
    public void LimitSlew_NegativeDeltaIsClamped()
    {
        var limited = OutputSafety.LimitSlew(0f, -1f, maxDelta: 0.2f);
        Assert.Equal(-0.2f, limited, precision: 4);
    }

    [Fact]
    public void LimitSlew_SmallChangesPassThrough()
    {
        var limited = OutputSafety.LimitSlew(0.1f, 0.15f, maxDelta: 0.2f);
        Assert.Equal(0.15f, limited, precision: 4);
    }

    [Fact]
    public void LimitSlew_ReachesTargetInSteps()
    {
        float value = 0f;
        for (int i = 0; i < 10; i++)
            value = OutputSafety.LimitSlew(value, 1f, maxDelta: 0.2f);
        Assert.Equal(1f, value, precision: 4);
    }

    [Fact]
    public void LimitSlew_NonFiniteTargetKeepsPrevious()
    {
        var limited = OutputSafety.LimitSlew(0.3f, float.NaN);
        Assert.Equal(0.3f, limited, precision: 4);
    }

    [Fact]
    public void LimitSlew_OutputStaysWithinRange()
    {
        var limited = OutputSafety.LimitSlew(0.9f, 1f, maxDelta: 0.5f);
        Assert.InRange(limited, -1f, 1f);
    }

    [Fact]
    public void IsStale_DetectsAge()
    {
        Assert.True(OutputSafety.IsStale(DateTimeOffset.UtcNow - TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2)));
        Assert.False(OutputSafety.IsStale(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2)));
    }
}