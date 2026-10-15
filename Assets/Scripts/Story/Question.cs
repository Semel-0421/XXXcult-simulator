using System;
using System.Collections.Generic;

// 개인 스탯 효과
public class StatEffect
{
    public PersonalStatId Stat { get; }
    public int Amount { get; }

    public StatEffect(PersonalStatId stat, int amount) { Stat = stat; Amount = amount; }
}

// 교단 스탯 효과
public readonly struct CultEffect
{
    public StatId Stat { get; }
    public int Amount { get; }

    public CultEffect(StatId stat, int amount) { Stat = stat; Amount = amount; }
}

// 선택지의 한 가지 결과(성공 또는 실패): 결과문, 스탯 변화, 지급할 기프티드.
public class Outcome
{
    readonly List<StatEffect> personal = new();
    readonly List<CultEffect> cult = new();
    readonly List<string> grants = new();

    public string Text { get; }
    public IReadOnlyList<StatEffect> Effects => personal;
    public IReadOnlyList<CultEffect> CultEffects => cult;
    public IReadOnlyList<string> Grants => grants;

    public Outcome(string text) { Text = text ?? ""; }

    public Outcome P(PersonalStatId stat, int amount) { personal.Add(new StatEffect(stat, amount)); return this; }
    public Outcome C(StatId stat, int amount) { cult.Add(new CultEffect(stat, amount)); return this; }
    public Outcome G(params string[] giftedIds) { grants.AddRange(giftedIds); return this; }
}

// 선택지. 성공 확률(0~1)로 성공/실패 결과 중 하나가 정해진다. 확률 1이면 항상 성공.
// 보정 스탯(BonusStat)이 있으면 그 개인 스탯(0~100)을 퍼센트로 바꿔 성공 확률에 더한다: 100이면 +MaxStatBonus.
public class Choice
{
    public const float MaxStatBonus = 0.15f;
    const float MinChance = 0.01f, MaxChance = 0.99f;

    public string Text { get; }
    public float Chance { get; }
    public PersonalStatId? BonusStat { get; private set; }
    public Outcome Success { get; }
    public Outcome Fail { get; }

    public Choice(string text, Outcome success) : this(text, 1f, success, null) { }

    public Choice(string text, float chance, Outcome success, Outcome fail)
    {
        if (chance < 1f && fail == null) throw new ArgumentException($"'{text}': 성공 확률이 1 미만이면 실패 결과가 필요함");
        Text = text; Chance = chance; Success = success; Fail = fail;
    }

    public Choice With(PersonalStatId bonusStat) { BonusStat = bonusStat; return this; }

    // 개인 스탯 보정을 더한 실제 성공 확률 (성공이 오르는 만큼 실패는 줄어든다)
    public float ChanceFor(PersonalStatus personal)
    {
        if (Chance >= 1f) return 1f;
        if (BonusStat is not { } stat || personal == null) return Chance;
        var bonus = personal[stat].Value / (float)PersonalStatus.MaxValue * MaxStatBonus;
        return Math.Clamp(Chance + bonus, MinChance, MaxChance);
    }

    public bool Roll(Random rng, PersonalStatus personal = null) => Chance >= 1f || rng.NextDouble() < ChanceFor(personal);

    public Outcome Resolve(bool success) => success || Fail == null ? Success : Fail;
}

public class Question
{
    public string Text { get; }
    public Choice[] Choices { get; }

    public Question(string text, params Choice[] choices) { Text = text; Choices = choices; }
}
