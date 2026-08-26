using ETS2LA.Shared;

namespace ETS2LA.Hardened.Tests;

public sealed class SampleHistoryTests
{
    [Fact]
    public void Add_KeepsOldestToNewestOrder()
    {
        var history = new SampleHistory(4);
        history.Add(1f);
        history.Add(2f);
        history.Add(3f);
        Assert.Equal(new[] { 1f, 2f, 3f }, history.ToArray());
    }

    [Fact]
    public void Add_OverflowsAndDropsOldest()
    {
        var history = new SampleHistory(3);
        history.Add(1f);
        history.Add(2f);
        history.Add(3f);
        history.Add(4f);
        Assert.Equal(new[] { 2f, 3f, 4f }, history.ToArray());
    }

    [Fact]
    public void Count_CappedAtCapacity()
    {
        var history = new SampleHistory(2);
        history.Add(1f);
        history.Add(2f);
        history.Add(3f);
        Assert.Equal(2, history.Count);
    }

    [Fact]
    public void Average_Min_Max()
    {
        var history = new SampleHistory(4);
        history.Add(10f);
        history.Add(20f);
        history.Add(30f);
        Assert.Equal(20f, history.Average(), precision: 4);
        Assert.Equal(10f, history.Min(), precision: 4);
        Assert.Equal(30f, history.Max(), precision: 4);
    }

    [Fact]
    public void Empty_ReturnsZero()
    {
        var history = new SampleHistory(4);
        Assert.Equal(0, history.Count);
        Assert.Equal(0f, history.Average(), precision: 4);
        Assert.Equal(0f, history.Min(), precision: 4);
        Assert.Equal(0f, history.Max(), precision: 4);
    }

    [Fact]
    public void Constructor_RejectsNonPositiveCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SampleHistory(0));
    }
}