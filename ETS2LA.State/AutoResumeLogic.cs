namespace ETS2LA.State;

/// <summary>
///  Pure decision logic for the "auto-resume after driver intervention"
///  behavior. Kept free of game state access so it can be unit tested.
/// </summary>
public static class AutoResumeLogic
{
    /// <summary>
    ///  Speed below which the truck counts as "stopped". A stopped truck must
    ///  never start moving on its own after an intervention pause; resuming
    ///  from standstill requires an explicit SET press.
    /// </summary>
    public const float MinAutoResumeSpeedMps = 0.5f;

    /// <summary>
    ///  Decides whether paused assists may be resumed automatically.
    /// </summary>
    /// <param name="enabled">User setting AutoResumeAfterIntervention.</param>
    /// <param name="interventionPaused">True when the pause was caused by driver intervention (not an explicit SET pause).</param>
    /// <param name="anyAssistPaused">True when steering or longitudinal assist is currently paused.</param>
    /// <param name="gamePaused">True when the game itself is paused (menu, photo mode...).</param>
    /// <param name="driverHandsOff">True when brake/throttle/steering inputs are all released.</param>
    /// <param name="secondsSincePause">Seconds since the intervention pause started (or since input was last held).</param>
    /// <param name="resumeDelaySeconds">Configured delay before an automatic resume.</param>
    /// <param name="speedMps">Current truck speed in m/s.</param>
    public static bool ShouldResume(
        bool enabled,
        bool interventionPaused,
        bool anyAssistPaused,
        bool gamePaused,
        bool driverHandsOff,
        double secondsSincePause,
        float resumeDelaySeconds,
        float speedMps)
    {
        if (!enabled) return false;
        if (!interventionPaused) return false; // explicit SET pauses never auto-resume
        if (!anyAssistPaused) return false;
        if (gamePaused) return false;
        if (!driverHandsOff) return false;
        if (secondsSincePause < resumeDelaySeconds) return false;
        if (speedMps < MinAutoResumeSpeedMps) return false; // never auto-start from standstill
        return true;
    }
}
