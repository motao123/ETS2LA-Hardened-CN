namespace ETS2LA.AutoBehavior;

/// <summary>
///  Pure decision math for AutoBehavior. No game state access, so it can be
///  unit tested without a running game.
/// </summary>
public static class AutoBehaviorLogic
{
    /// <summary>
    ///  Brake intensity (0..1) needed to bring the truck to a stop before a
    ///  closed barrier. Returns 0 when no braking is required.
    /// </summary>
    /// <param name="speedMps">Current speed in m/s.</param>
    /// <param name="distanceToGateM">Straight-line distance to the barrier in meters.</param>
    /// <param name="stopDistanceM">Gap to keep in front of the barrier in meters.</param>
    /// <param name="maxDecelerationMps2">Hardest deceleration the plugin may request.</param>
    /// <param name="holdBrake">Brake intensity used to hold the truck at the barrier.</param>
    public static float ComputeTollBrake(
        float speedMps,
        float distanceToGateM,
        float stopDistanceM,
        float maxDecelerationMps2,
        float holdBrake)
    {
        const float minBrake = 0.02f;

        if (distanceToGateM <= stopDistanceM)
        {
            // At the barrier: brake hard while rolling, hold gently once stopped.
            float hold = speedMps > 0.1f ? 1f : holdBrake;
            return hold < minBrake ? 0f : hold;
        }

        float effective = MathF.Max(distanceToGateM - stopDistanceM, 0.1f);
        float requiredDecel = speedMps > 0.3f ? (speedMps * speedMps) / (2f * effective) : 0f;
        float brake = Math.Clamp(requiredDecel / maxDecelerationMps2, 0f, 1f);
        return brake < minBrake ? 0f : brake;
    }

    /// <summary>
    ///  True when the truck needs a refueling stop soon.
    /// </summary>
    public static bool IsFuelLow(
        float fuelLiters,
        float capacityLiters,
        float rangeMeters,
        float fractionThreshold,
        float rangeThresholdMeters)
    {
        if (capacityLiters <= 0f) return false;
        float fraction = fuelLiters / capacityLiters;
        return fraction < fractionThreshold || (rangeMeters > 0f && rangeMeters < rangeThresholdMeters);
    }
}
