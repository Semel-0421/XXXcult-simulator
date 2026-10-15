using System;
using System.Collections.Generic;

// 교단 창설 후 이벤트.
//  - 확률 이벤트: 관련 교단 스탯(Theme)이 높을수록 잘 나온다.
//  - 조건 이벤트: 조건을 만족하면 한 번 발동한다.
//  - 부정적 이벤트: 교단 3년차부터 해마다 조금씩 높아지는 확률로, 어떤 스탯이 바닥에 가까울 때(조건) 닥친다.
public class CultEvent
{
    public enum EventKind { Random, Conditional, Negative }

    public const int NegativeFirstYear = 3;          // 이 교단 연차부터 부정적 이벤트가 가능
    public const float NegativeBaseChance = 0.08f;   // 첫 해 발생 확률
    public const float NegativePerYear = 0.03f;      // 해마다 늘어나는 확률
    public const float NegativeMaxChance = 0.50f;

    public const float Chance = 0.6f;       // 행동 후 확률 이벤트 발생 확률
    const float BaseWeight = 0.2f;          // 스탯이 0이어도 나올 수 있게 깔아 두는 가중치

    public string Id { get; }
    public StatId Theme { get; }
    public Func<CultStatus, bool> Condition { get; }
    public Question Question { get; }
    public EventKind Kind { get; }
    public bool IsConditional => Kind == EventKind.Conditional;
    public bool IsNegative => Kind == EventKind.Negative;

    CultEvent(string id, StatId theme, Func<CultStatus, bool> condition, Question question, EventKind kind)
    {
        Id = id; Theme = theme; Condition = condition; Question = question; Kind = kind;
    }

    public static CultEvent Random(string id, StatId theme, Question question) => new(id, theme, null, question, EventKind.Random);
    public static CultEvent Conditional(string id, Func<CultStatus, bool> condition, Question question) => new(id, default, condition, question, EventKind.Conditional);
    public static CultEvent Negative(string id, Func<CultStatus, bool> condition, Question question) => new(id, default, condition, question, EventKind.Negative);

    // 교단 연차(1년차부터)에 따른 부정적 이벤트 발생 확률. 3년차 전에는 0.
    public static float NegativeChanceAt(int cultYear) => cultYear < NegativeFirstYear
        ? 0f
        : Math.Min(NegativeMaxChance, NegativeBaseChance + NegativePerYear * (cultYear - NegativeFirstYear));

    // 조건에 맞는(스탯이 바닥에 가까운) 아직 안 나온 부정적 이벤트 중 하나를 확률적으로 뽑는다. 없으면 null.
    public static CultEvent RollNegative(IReadOnlyList<CultEvent> pool, CultStatus cult, ISet<string> used, Random rng, int cultYear)
    {
        var chance = NegativeChanceAt(cultYear);
        if (chance <= 0f || rng.NextDouble() >= chance) return null;

        var candidates = new List<CultEvent>();
        foreach (var e in pool)
            if (e.IsNegative && !used.Contains(e.Id) && e.Condition(cult)) candidates.Add(e);
        if (candidates.Count == 0) return null;

        var picked = candidates[rng.Next(candidates.Count)];
        used.Add(picked.Id);
        return picked;
    }

    // 조건을 만족하는 아직 안 나온 조건 이벤트 (목록 순서대로 하나)
    public static CultEvent FindTriggered(IReadOnlyList<CultEvent> pool, CultStatus cult, ISet<string> used)
    {
        foreach (var e in pool)
            if (e.IsConditional && !used.Contains(e.Id) && e.Condition(cult)) { used.Add(e.Id); return e; }
        return null;
    }

    // Chance 확률로 확률 이벤트를 뽑는다 (스탯 비율 가중치, 안 나온 것 우선). 발생하지 않으면 null.
    public static CultEvent RollRandom(IReadOnlyList<CultEvent> pool, CultStatus cult, ISet<string> used, Random rng)
    {
        if (rng.NextDouble() >= Chance) return null;
        var picked = PickWeighted(pool, cult, used, rng, skipUsed: true) ?? PickWeighted(pool, cult, used, rng, skipUsed: false);
        if (picked != null) used.Add(picked.Id);
        return picked;
    }

    static CultEvent PickWeighted(IReadOnlyList<CultEvent> pool, CultStatus cult, ISet<string> used, Random rng, bool skipUsed)
    {
        bool Eligible(CultEvent e) => e.Kind == EventKind.Random && !(skipUsed && used.Contains(e.Id));

        float total = 0;
        foreach (var e in pool)
            if (Eligible(e)) total += BaseWeight + cult.Ratio(e.Theme);
        if (total <= 0) return null;

        var roll = (float)rng.NextDouble() * total;
        CultEvent last = null;
        foreach (var e in pool)
        {
            if (!Eligible(e)) continue;
            last = e;
            roll -= BaseWeight + cult.Ratio(e.Theme);
            if (roll <= 0) return e;
        }
        return last;
    }
}
