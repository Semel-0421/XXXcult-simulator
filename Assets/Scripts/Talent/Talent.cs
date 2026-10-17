// 기프티드: 스탯 퍼센트 보정(Modifiers)과 보유 특수효과(Specials)를 가진다. 목록은 GiftedCatalog.
public class Talent
{
    public string Id { get; }
    readonly string name, description, extraEffect;
    public string Name => Lang.Pick(name);
    public string Category { get; }
    public string Description => Lang.Pick(description);
    public string ExtraEffect => Lang.Pick(extraEffect);
    public StatModifier[] Modifiers { get; }
    public GiftedSpecial[] Specials { get; }

    public Talent(string id, string name, string category, string description, string extraEffect, StatModifier[] modifiers, params GiftedSpecial[] specials)
    {
        Id = id; this.name = name; Category = category; this.description = description; this.extraEffect = extraEffect; Modifiers = modifiers; Specials = specials;
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
