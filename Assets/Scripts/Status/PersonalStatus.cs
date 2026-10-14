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

    public ClampedStat this[PersonalStatId id] => stats[id];
}
