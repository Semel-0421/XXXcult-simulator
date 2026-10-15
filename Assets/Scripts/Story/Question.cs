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
public class Choice
{
    public string Text { get; }
    public float Chance { get; }
    public Outcome Success { get; }
    public Outcome Fail { get; }

    public Choice(string text, Outcome success) : this(text, 1f, success, null) { }

    public Choice(string text, float chance, Outcome success, Outcome fail)
    {
        if (chance < 1f && fail == null) throw new ArgumentException($"'{text}': 성공 확률이 1 미만이면 실패 결과가 필요함");
        Text = text; Chance = chance; Success = success; Fail = fail;
    }

    public bool Roll(Random rng) => Chance >= 1f || rng.NextDouble() < Chance;

    public Outcome Resolve(bool success) => success || Fail == null ? Success : Fail;
}

public class Question
{
    public string Text { get; }
    public Choice[] Choices { get; }

    public Question(string text, params Choice[] choices) { Text = text; Choices = choices; }
}
