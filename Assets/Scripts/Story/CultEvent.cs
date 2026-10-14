using System;
using System.Collections.Generic;

// 교단 창설 후 이벤트. 확률 이벤트는 관련 교단 스탯(Theme)이 높을수록 잘 나오고, 조건 이벤트는 조건을 만족하면 한 번 발동한다.
public class CultEvent
{
    public const float Chance = 0.6f;       // 행동 후 확률 이벤트 발생 확률
    const float BaseWeight = 0.2f;          // 스탯이 0이어도 나올 수 있게 깔아 두는 가중치

    public string Id { get; }
    public StatId Theme { get; }
    public Func<CultStatus, bool> Condition { get; }
    public Question Question { get; }
    public bool IsConditional => Condition != null;

    CultEvent(string id, StatId theme, Func<CultStatus, bool> condition, Question question)
    {
        Id = id; Theme = theme; Condition = condition; Question = question;
    }

    public static CultEvent Random(string id, StatId theme, Question question) => new(id, theme, null, question);
    public static CultEvent Conditional(string id, Func<CultStatus, bool> condition, Question question) => new(id, default, condition, question);

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
        bool Eligible(CultEvent e) => !e.IsConditional && !(skipUsed && used.Contains(e.Id));

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
