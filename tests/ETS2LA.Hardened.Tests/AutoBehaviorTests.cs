using ETS2LA.AutoBehavior;
using ETS2LA.State;

namespace ETS2LA.Hardened.Tests;

public sealed class TollBrakeTests
{
    private const float StopDistance = 12f;
    private const float MaxDecel = 2.6f;
    private const float HoldBrake = 0.5f;

    [Fact]
    public void FarAwayAndSlow_ReturnsOnlyTraceBrake()
    {
        float brake = AutoBehaviorLogic.ComputeTollBrake(5f, 150f, StopDistance, MaxDecel, HoldBrake);
        Assert.True(brake < 0.05f, $"expected a trace value, got {brake}");
    }

    [Fact]
    public void RequiredDecelScalesWithSpeedSquaredOverDistance()
    {
        // 10 m/s: effective 88 m needs 0.22 brake, effective 28 m needs 0.69.
        float far = AutoBehaviorLogic.ComputeTollBrake(10f, 100f, StopDistance, MaxDecel, HoldBrake);
        float closer = AutoBehaviorLogic.ComputeTollBrake(10f, 40f, StopDistance, MaxDecel, HoldBrake);
        Assert.True(closer > far, "braking must intensify as the gate approaches");
        Assert.True(closer <= 1f);
    }

    [Fact]
    public void AtTheBarrierWhileRolling_BrakesFully()
    {
        float brake = AutoBehaviorLogic.ComputeTollBrake(3f, 8f, StopDistance, MaxDecel, HoldBrake);
        Assert.Equal(1f, brake);
    }

    [Fact]
    public void AtTheBarrierWhenStopped_HoldsGentleBrake()
    {
        float brake = AutoBehaviorLogic.ComputeTollBrake(0f, 8f, StopDistance, MaxDecel, HoldBrake);
        Assert.Equal(HoldBrake, brake);
    }

    [Fact]
    public void ResultNeverExceedsOne()
    {
        // Extremely close and fast: required decel far exceeds MaxDecel.
        float brake = AutoBehaviorLogic.ComputeTollBrake(25f, 15f, StopDistance, MaxDecel, HoldBrake);
        Assert.True(brake <= 1f && brake >= 0f);
    }
}

public sealed class FuelWatchdogTests
{
    [Fact]
    public void BelowFractionThreshold_IsLow()
    {
        Assert.True(AutoBehaviorLogic.IsFuelLow(10f, 100f, 500_000f, 0.15f, 120_000f));
    }

    [Fact]
    public void BelowRangeThreshold_IsLow()
    {
        // 30% remaining but only 100 km of range left.
        Assert.True(AutoBehaviorLogic.IsFuelLow(30f, 100f, 100_000f, 0.15f, 120_000f));
    }

    [Fact]
    public void HealthyTank_IsNotLow()
    {
        Assert.False(AutoBehaviorLogic.IsFuelLow(50f, 100f, 400_000f, 0.15f, 120_000f));
    }

    [Fact]
    public void UnknownCapacity_IsNeverLow()
    {
        Assert.False(AutoBehaviorLogic.IsFuelLow(0f, 0f, 0f, 0.15f, 120_000f));
    }
}

public sealed class AutoResumeTests
{
    private static bool Resume(
        bool enabled = true,
        bool interventionPaused = true,
        bool anyAssistPaused = true,
        bool gamePaused = false,
        bool handsOff = true,
        double seconds = 4f,
        float delay = 3f,
        float speed = 10f)
    {
        return AutoResumeLogic.ShouldResume(enabled, interventionPaused, anyAssistPaused,
            gamePaused, handsOff, seconds, delay, speed);
    }

    [Fact]
    public void ReleasedInputAfterDelay_Resumes()
    {
        Assert.True(Resume());
    }

    [Fact]
    public void ExplicitSetPause_NeverAutoResumes()
    {
        Assert.False(Resume(interventionPaused: false));
    }

    [Fact]
    public void DisabledSetting_NeverAutoResumes()
    {
        Assert.False(Resume(enabled: false));
    }

    [Fact]
    public void StillWithinDelay_DoesNotResume()
    {
        Assert.False(Resume(seconds: 1f));
    }

    [Fact]
    public void GamePaused_DoesNotResume()
    {
        Assert.False(Resume(gamePaused: true));
    }

    [Fact]
    public void HandsStillOnInput_DoesNotResume()
    {
        Assert.False(Resume(handsOff: false));
    }

    [Fact]
    public void TruckStopped_DoesNotResume()
    {
        // Standing still after an intervention must never self-start;
        // launching from standstill requires an explicit SET press.
        Assert.False(Resume(speed: 0f));
    }
}
