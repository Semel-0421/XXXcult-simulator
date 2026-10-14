using System;
using System.Collections.Generic;
using System.Linq;

public enum Gender { Male, Female }

public class PersonalStatus
{
    public const int MaxValue = 100;

    public string Name { get; set; } = "";
    public Gender Gender { get; set; }
    public TalentList Talents { get; } = new();

    readonly Dictionary<PersonalStatId, ClampedStat> stats =
        ((PersonalStatId[])Enum.GetValues(typeof(PersonalStatId))).ToDictionary(id => id, _ => new ClampedStat(max: MaxValue));

    readonly RemainderBank<PersonalStatId> remainders = new();

    public ClampedStat this[PersonalStatId id] => stats[id];
    public RemainderBank<PersonalStatId> Remainders => remainders;

    // 퍼센트 보정으로 생긴 소수는 쌓아 두었다가 반올림되면 반영한다
    public void AddWithRemainder(PersonalStatId id, float amount) => stats[id].Add(remainders.Take(id, amount));
}
