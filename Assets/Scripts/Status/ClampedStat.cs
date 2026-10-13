using System;

public class ClampedStat
{
    public const int DefaultMax = 1000;

    public int Min { get; }
    public int Max { get; }
    public int Value { get; private set; }
    public event Action<int> Changed;

    public ClampedStat(int startValue = 0, int max = DefaultMax, int min = 0)
    {
        Min = min;
        Max = max;
        Value = Math.Clamp(startValue, min, max);
    }

    public void Set(int newValue)
    {
        newValue = Math.Clamp(newValue, Min, Max);
        if (newValue == Value) return;
        Value = newValue;
        Changed?.Invoke(Value);
    }

    public void Add(int amount) => Set((int)Math.Clamp((long)Value + amount, Min, Max));
}
