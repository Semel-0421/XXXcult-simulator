using System;
using System.Collections.Generic;

// 퍼센트 보정으로 생긴 소수 잔여분을 키별로 모아 두고, 반올림해 정수만큼 꺼내 준다.
public class RemainderBank<TKey>
{
    readonly Dictionary<TKey, float> carried = new();

    public int Take(TKey key, float amount)
    {
        carried.TryGetValue(key, out var previous);
        var total = amount + previous;
        var whole = (int)Math.Round(total, MidpointRounding.AwayFromZero);
        carried[key] = total - whole;
        return whole;
    }
}
