// 기프티드: 행동에 발생하는 수치를 조정하거나 확률을 보정하는 스킬.
// 목록은 GiftedCatalog에 있고, 수치 효과는 Modifiers(퍼센트)로 보관한다.
public class Talent
{
    public string Id { get; }
    public string Name { get; }
    public string Category { get; }
    public string Description { get; }
    public string ExtraEffect { get; }
    public StatModifier[] Modifiers { get; }

    public Talent(string id, string name, string category, string description, string extraEffect, params StatModifier[] modifiers)
    {
        Id = id; Name = name; Category = category; Description = description; ExtraEffect = extraEffect; Modifiers = modifiers;
    }
}

// 교단 스탯(StatId) 또는 개인 스탯(PersonalStatId) 중 하나에 거는 퍼센트 보정.
public readonly struct StatModifier
{
    public StatId? CultStat { get; }
    public PersonalStatId? PersonalStat { get; }
    public float Percent { get; }

    public StatModifier(StatId stat, float percent) { CultStat = stat; PersonalStat = null; Percent = percent; }
    public StatModifier(PersonalStatId stat, float percent) { CultStat = null; PersonalStat = stat; Percent = percent; }
}
