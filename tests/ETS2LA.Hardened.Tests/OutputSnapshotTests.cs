using ETS2LA.Game.Output;

namespace ETS2LA.Hardened.Tests;

public sealed class OutputSnapshotTests
{
    [Fact]
    public void Record_StoresValuesAndMarksHasData()
    {
        var snap = new OutputSnapshot();
        snap.Record(0.25f, -0.5f);
        Assert.True(snap.HasData);
        Assert.Equal(0.25f, snap.Steering, precision: 4);
        Assert.Equal(-0.5f, snap.Acceleration, precision: 4);
    }

    [Fact]
    public void Record_ClampsOutOfRangeValues()
    {
        var snap = new OutputSnapshot();
        snap.Record(2f, -2f);
        Assert.Equal(1f, snap.Steering, precision: 4);
        Assert.Equal(-1f, snap.Acceleration, precision: 4);
    }

    [Fact]
    public void IsStale_NoDataReturnsTrue()
    {
        Assert.True(new OutputSnapshot().IsStale(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public void IsStale_InvalidAgeWithDataReturnsFalse()
    {
        var snap = new OutputSnapshot();
        snap.Record(0f, 0f);
        Assert.False(snap.IsStale(TimeSpan.Zero));
    }

    [Fact]
    public void Record_OverwritesPreviousValues()
    {
        var snap = new OutputSnapshot();
        snap.Record(0.1f, 0.2f);
        snap.Record(-0.7f, 0.8f);
        Assert.Equal(-0.7f, snap.Steering, precision: 4);
        Assert.Equal(0.8f, snap.Acceleration, precision: 4);
    }
}