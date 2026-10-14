using System;
using System.Linq;

public enum PersonalStatGroup { Vitals, Miracle, Desire, Virtue }

public static class PersonalStatInfo
{
    public static PersonalStatGroup GroupOf(PersonalStatId id) => id switch
    {
        PersonalStatId.Wealth or <= PersonalStatId.Repute => PersonalStatGroup.Vitals,
        PersonalStatId.Miracle => PersonalStatGroup.Miracle,
        <= PersonalStatId.Ambition => PersonalStatGroup.Desire,
        _ => PersonalStatGroup.Virtue,
    };

    public static bool IsHidden(PersonalStatId id) => id == PersonalStatId.Miracle;

    public static PersonalStatId[] InGroup(PersonalStatGroup group) =>
        ((PersonalStatId[])Enum.GetValues(typeof(PersonalStatId))).Where(id => GroupOf(id) == group).ToArray();
}
