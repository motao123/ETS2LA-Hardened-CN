using ETS2LA.Game.Output;

namespace ETS2LA.Hardened.Tests;

public sealed class ControlMixerTests
{
    [Fact]
    public void Mix_UsesPositiveWeightsAndClampsResult()
    {
        var channels = new[]
        {
            Channel("a", 1f, steering: 2f),
            Channel("b", 3f, steering: 1f)
        };

        var result = ControlMixer.Mix(channels);

        Assert.Equal(1f, result["steering"]);
    }

    [Fact]
    public void Mix_IgnoresInvalidValuesAndWeights()
    {
        var channels = new[]
        {
            Channel("invalid-weight", float.NaN, steering: 1f),
            Channel("invalid-value", 1f, steering: float.PositiveInfinity),
            Channel("valid", 2f, steering: 0.25f)
        };

        var result = ControlMixer.Mix(channels);

        Assert.Equal(0.25f, result["steering"]);
    }

    [Fact]
    public void Mix_MapsBackwardAccelerationToNegativeValue()
    {
        var channels = new[] { Channel("brake", 1f, abackward: 0.75f) };

        var result = ControlMixer.Mix(channels);

        Assert.Equal(-0.75f, result["acceleration"]);
    }

    [Fact]
    public void TryMix_ReturnsFalseWhenNoFinitePositiveContributionExists()
    {
        var ok = ControlMixer.TryMix(
            new[] { new WeightedValue(0f, 1f), new WeightedValue(float.NaN, 1f) },
            out var result);

        Assert.False(ok);
        Assert.Equal(0f, result);
    }

    private static ControlChannel Channel(string id, float weight, float? steering = null, float? abackward = null)
    {
        var variables = new ControlVariables
        {
            steering = steering,
            abackward = abackward
        };
        return new ControlChannel
        {
            Definition = new ControlChannelDefinition { Id = id, Timeout = 1f },
            Properties = new ControlProperties { Weight = weight },
            Variables = variables
        };
    }
}
