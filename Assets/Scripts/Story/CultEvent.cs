using System;
using System.Collections.Generic;

// 교단 창설 후 이벤트.
//  - 확률 이벤트: 관련 교단 스탯(Theme)이 높을수록 잘 나온다.
//  - 조건 이벤트: 조건을 만족하면 한 번 발동한다.
//  - 부정적 이벤트: 교단 3년차부터 해마다 조금씩 높아지는 확률로, 어떤 스탯이 바닥에 가까울 때(조건) 닥친다.
//  - 긍정 이벤트(무드): 개인의 욕망 또는 미덕 평균이 높을 때만 나오며, 모든 선택지 효과가 0 이상이다.
public class CultEvent
{
    public enum EventKind { Random, Conditional, Negative, Mood }

    // 긍정 이벤트를 고르는 기준: 욕망 4가지 평균 또는 미덕 4가지 평균
    public enum MoodTrait { Desire, Virtue }

    static readonly PersonalStatId[] DesireStats = { PersonalStatId.Lust, PersonalStatId.Greed, PersonalStatId.Appetite, PersonalStatId.Ambition };
    static readonly PersonalStatId[] VirtueStats = { PersonalStatId.Temperance, PersonalStatId.Charity, PersonalStatId.Patience, PersonalStatId.Integrity };

    public const float MoodChance = 0.03f;     // 평균이 MoodRollAbove를 넘은 뒤, 이벤트가 열릴 때 긍정 이벤트가 뽑힐 확률
    public const float MoodRollAbove = 30f;    // 이 값을 넘어야 확률 계산을 시작한다
    public static readonly int[] MoodMilestones = { 50, 20 }; // 평균이 이 값에 이르면 각각 한 번씩 확정 발생 (큰 값부터 확인)

    // 확정 발생을 이미 썼는지 기록하는 키 (저장되는 UsedEvents에 함께 담긴다)
    public static string MilestoneKey(MoodTrait trait, int milestone) => $"mood_{trait}_{milestone}";

    public const int NegativeFirstYear = 3;          // 이 교단 연차부터 부정적 이벤트가 가능
    public const float NegativeBaseChance = 0.08f;   // 첫 해 발생 확률
    public const float NegativePerYear = 0.03f;      // 해마다 늘어나는 확률
    public const float NegativeMaxChance = 0.50f;

    public const float Chance = 0.6f;       // 행동 후 확률 이벤트 발생 확률
    const float BaseWeight = 0.2f;          // 스탯이 0이어도 나올 수 있게 깔아 두는 가중치
    // 명망이 이 선 아래로 내려가면 100 단위마다 명망 테마 이벤트(명망을 올리는 선택지가 많다) 가중치를 더한다: 399~300 1단계 … 99~0 4단계
    public const int ReputationBoostLine = 400, ReputationBoostStep = 100;
    public const float ReputationBoostPerStep = 2f;

    public static int ReputationBoostSteps(int reputation) =>
        reputation >= ReputationBoostLine ? 0 : (ReputationBoostLine - System.Math.Max(0, reputation) + ReputationBoostStep - 1) / ReputationBoostStep;

    static float Weight(CultEvent e, CultStatus cult) => BaseWeight + cult.Ratio(e.Theme)
        + (e.Theme == StatId.Reputation ? ReputationBoostPerStep * ReputationBoostSteps(cult[StatId.Reputation].Value) : 0f);

    public string Id { get; }
    public StatId Theme { get; }
    public Func<CultStatus, bool> Condition { get; }
    public Question Question { get; }
    public EventKind Kind { get; }
    public bool IsConditional => Kind == EventKind.Conditional;
    public bool IsNegative => Kind == EventKind.Negative;
    public bool IsMood => Kind == EventKind.Mood;
    public MoodTrait Trait { get; private set; }

    CultEvent(string id, StatId theme, Func<CultStatus, bool> condition, Question question, EventKind kind)
    {
        Id = id; Theme = theme; Condition = condition; Question = question; Kind = kind;
    }

    public static CultEvent Random(string id, StatId theme, Question question) => new(id, theme, null, question, EventKind.Random);
    public static CultEvent Conditional(string id, Func<CultStatus, bool> condition, Question question) => new(id, default, condition, question, EventKind.Conditional);
    public static CultEvent Negative(string id, Func<CultStatus, bool> condition, Question question) => new(id, default, condition, question, EventKind.Negative);

    public static CultEvent Mood(string id, MoodTrait trait, Question question) => new(id, default, null, question, EventKind.Mood) { Trait = trait };

    public static float TraitAverage(PersonalStatus personal, MoodTrait trait)
    {
        var stats = trait == MoodTrait.Desire ? DesireStats : VirtueStats;
        float sum = 0;
        foreach (var id in stats) sum += personal[id].Value;
        return sum / stats.Length;
    }

    // 긍정 이벤트: 욕망·미덕 각각의 평균이 50, 20에 처음 이르면 확정으로 한 번씩 발생하고,
    // 더 높은 쪽 평균이 30을 넘으면 그 뒤로는 MoodChance 확률로 아직 안 나온 이벤트를 하나 뽑는다.
    public static CultEvent RollMood(IReadOnlyList<CultEvent> pool, PersonalStatus personal, ISet<string> used, Random rng)
    {
        float desire = TraitAverage(personal, MoodTrait.Desire), virtue = TraitAverage(personal, MoodTrait.Virtue);

        foreach (var milestone in MoodMilestones)
            foreach (var trait in new[] { MoodTrait.Desire, MoodTrait.Virtue })
            {
                var average = trait == MoodTrait.Desire ? desire : virtue;
                var key = MilestoneKey(trait, milestone);
                if (average < milestone || used.Contains(key)) continue;
                used.Add(key);
                var guaranteed = PickMood(pool, trait, used, rng);
                if (guaranteed != null) return guaranteed;
            }

        var higher = desire >= virtue ? MoodTrait.Desire : MoodTrait.Virtue;
        if ((higher == MoodTrait.Desire ? desire : virtue) <= MoodRollAbove) return null;
        if (rng.NextDouble() >= MoodChance) return null;
        return PickMood(pool, higher, used, rng);
    }

    static CultEvent PickMood(IReadOnlyList<CultEvent> pool, MoodTrait trait, ISet<string> used, Random rng)
    {
        var candidates = new List<CultEvent>();
        foreach (var e in pool)
            if (e.IsMood && e.Trait == trait && !used.Contains(e.Id)) candidates.Add(e);
        if (candidates.Count == 0) return null;
        var picked = candidates[rng.Next(candidates.Count)];
        used.Add(picked.Id);
        return picked;
    }

    // 교단 연차(1년차부터)에 따른 부정적 이벤트 발생 확률. 3년차 전에는 0.
    public static float NegativeChanceAt(int cultYear) => cultYear < NegativeFirstYear
        ? 0f
        : Math.Min(NegativeMaxChance, NegativeBaseChance + NegativePerYear * (cultYear - NegativeFirstYear));

    // 조건에 맞는(스탯이 바닥에 가까운) 아직 안 나온 부정적 이벤트 중 하나를 확률적으로 뽑는다. 없으면 null.
    public static CultEvent RollNegative(IReadOnlyList<CultEvent> pool, CultStatus cult, ISet<string> used, Random rng, int cultYear) =>
        RollNegative(pool, cult, used, rng, NegativeChanceAt(cultYear));

    // 준비된 신 모드는 연차 대신 압박 단계로 확률을 정한다
    public static CultEvent RollNegative(IReadOnlyList<CultEvent> pool, CultStatus cult, ISet<string> used, Random rng, float chance)
    {
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
            if (Eligible(e)) total += Weight(e, cult);
        if (total <= 0) return null;

        var roll = (float)rng.NextDouble() * total;
        CultEvent last = null;
        foreach (var e in pool)
        {
            if (!Eligible(e)) continue;
            last = e;
            roll -= Weight(e, cult);
            if (roll <= 0) return e;
        }
        return last;
    }
}
