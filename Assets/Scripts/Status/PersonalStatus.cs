using System;
using System.Collections.Generic;
using System.Linq;

public class PersonalStatus
{
    public const int MaxValue = 100;

    public string Name { get; set; } = "";
    public TalentList Talents { get; } = new();

    readonly Dictionary<PersonalStatId, ClampedStat> stats =
        ((PersonalStatId[])Enum.GetValues(typeof(PersonalStatId))).ToDictionary(id => id, _ => new ClampedStat(max: MaxValue));

    // 퍼센트 보정으로 생긴 소수 잔여분. 쌓여서 반올림되면 스탯에 반영된다.
    readonly Dictionary<PersonalStatId, float> remainders = new();

    public ClampedStat this[PersonalStatId id] => stats[id];

    public void AddWithRemainder(PersonalStatId id, float amount)
    {
        remainders.TryGetValue(id, out var carried);
        var total = amount + carried;
        var whole = (int)Math.Round(total, MidpointRounding.AwayFromZero);
        remainders[id] = total - whole;
        stats[id].Add(whole);
    }
}
