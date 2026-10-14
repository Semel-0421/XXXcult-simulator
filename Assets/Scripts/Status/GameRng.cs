using System;

// 상태를 저장·복원할 수 있는 난수 생성기 (SplitMix64). 세이브 후 불러와도 같은 순서로 난수가 이어진다.
public sealed class GameRng : Random
{
    const ulong Gamma = 0x9E3779B97F4A7C15UL;

    ulong state;

    public ulong State => state;

    public GameRng(int seed) : base(0) => state = (ulong)(uint)seed * Gamma + 0x5DEECE66DUL;

    public GameRng(ulong state) : base(0) => this.state = state;

    ulong NextBits()
    {
        var z = state += Gamma;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    protected override double Sample() => (NextBits() >> 11) * (1.0 / (1UL << 53));

    public override double NextDouble() => Sample();

    public override int Next() => (int)(NextBits() >> 33);

    public override int Next(int maxValue)
    {
        if (maxValue < 0) throw new ArgumentOutOfRangeException(nameof(maxValue));
        return (int)(Sample() * maxValue);
    }

    public override int Next(int minValue, int maxValue)
    {
        if (minValue > maxValue) throw new ArgumentOutOfRangeException(nameof(minValue));
        return minValue + (int)(Sample() * ((long)maxValue - minValue));
    }
}
