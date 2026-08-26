using ETS2LA.Shared;

namespace ETS2LA.Hardened.Tests;

public sealed class SpeedUnitConverterTests
{
    [Theory]
    [InlineData(90f, 25f)]      // 90 km/h -> 25 m/s
    [InlineData(0f, 0f)]
    [InlineData(36f, 10f)]
    public void ToMetersPerSecond_Metric(float kmh, float expectedMps)
        => Assert.Equal(expectedMps, SpeedUnitConverter.ToMetersPerSecond(kmh, SpeedUnit.Metric), precision: 4);

    [Theory]
    [InlineData(67.108f, 30f)]  // ~67.1 mph -> 30 m/s
    [InlineData(10f, 4.4704f)]
    public void ToMetersPerSecond_Imperial(float mph, float expectedMps)
        => Assert.Equal(expectedMps, SpeedUnitConverter.ToMetersPerSecond(mph, SpeedUnit.Imperial), precision: 4);

    [Fact]
    public void ToMetersPerSecond_ScientificIsIdentity()
        => Assert.Equal(25f, SpeedUnitConverter.ToMetersPerSecond(25f, SpeedUnit.Scientific));

    [Fact]
    public void FromMetersPerSecond_Metric()
        => Assert.Equal(90f, SpeedUnitConverter.FromMetersPerSecond(25f, SpeedUnit.Metric), precision: 4);

    [Fact]
    public void FromMetersPerSecond_Imperial()
        => Assert.Equal(67.108f, SpeedUnitConverter.FromMetersPerSecond(30f, SpeedUnit.Imperial), precision: 2);

    [Fact]
    public void RoundTrip_Metric()
    {
        const float mps = 25f;
        var display = SpeedUnitConverter.FromMetersPerSecond(mps, SpeedUnit.Metric);
        Assert.Equal(mps, SpeedUnitConverter.ToMetersPerSecond(display, SpeedUnit.Metric), precision: 4);
    }
}