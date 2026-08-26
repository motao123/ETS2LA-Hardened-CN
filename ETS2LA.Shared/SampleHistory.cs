namespace ETS2LA.Shared;

/// <summary>
/// 固定容量的环形缓冲，保存最近 N 个采样，按“最旧 → 最新”顺序返回，
/// 并提供 min / max / avg，供性能页渲染趋势图使用。
/// </summary>
public sealed class SampleHistory
{
    private readonly float[] _samples;
    private int _index;
    private int _count;

    public int Capacity => _samples.Length;
    public int Count => _count;

    public SampleHistory(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");
        _samples = new float[capacity];
    }

    public void Add(float value)
    {
        _samples[_index] = value;
        _index = (_index + 1) % _samples.Length;
        if (_count < _samples.Length)
            _count++;
    }

    /// <summary>返回从最旧到最新的采样数组。</summary>
    public float[] ToArray()
    {
        var result = new float[_count];
        var start = (_index - _count + _samples.Length) % _samples.Length;
        for (var i = 0; i < _count; i++)
            result[i] = _samples[(start + i) % _samples.Length];
        return result;
    }

    public float Average()
    {
        if (_count == 0)
            return 0f;
        double sum = 0;
        for (var i = 0; i < _count; i++)
            sum += _samples[i];
        return (float)(sum / _count);
    }

    public float Min()
    {
        if (_count == 0)
            return 0f;
        var min = _samples[0];
        for (var i = 1; i < _count; i++)
            min = Math.Min(min, _samples[i]);
        return min;
    }

    public float Max()
    {
        if (_count == 0)
            return 0f;
        var max = _samples[0];
        for (var i = 1; i < _count; i++)
            max = Math.Max(max, _samples[i]);
        return max;
    }
}