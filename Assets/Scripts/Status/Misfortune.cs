using System;
using System.Collections.Generic;

// only main achievements may influence misfortune — 불운은 메인 업적만 좌우할 수 있다.
//   히든 업적·기프티드 보상·모드별 업적은 불운의 쌓임/저항/쇠퇴에 닿으면 안 된다. 업적이 불운에 닿는 길은 Legacy.Level 의 업적 수 하나뿐이다.
// 불운: 부정적 이벤트가 닥칠 때마다 한 겹씩 쌓이고, 쌓인 만큼 매 턴 교단 스탯이 깎인다. 판이 끝날 때까지 사라지지 않는다.
// 계승(Legacy)의 불운 저항이 깎이는 양을 줄여 준다.
public static class Misfortune
{
    public const int MaxStacks = 10;

    // 불운 한 겹당 매 턴 깎이는 양 (소수는 쌓아 두었다가 반영)
    static readonly (StatId stat, float perStack)[] Decay =
    {
        (StatId.Reputation, 4.5f), // 다른 스탯보다 무겁게: 패배가 스탯 바닥보다 해체(명망 0)로 더 자주 오게
        (StatId.FollowerFaith, 2.5f),
        (StatId.LeaderFaith, 3f),
        (StatId.Authority, 3f),
        (StatId.Offerings, 2.5f),
        (StatId.Doctrine, 2f),
        (StatId.Followers, 0.5f),
    };

    // 겹이 쌓일수록 한 겹의 무게는 줄어든다: 실제로 치는 겹 수 = log2(1 + 겹) → 1겹 1, 3겹 2, 7겹 3, 10겹 약 3.46
    public const float Strength = 0.8f; // 불운 디버프 전체 세기 (20% 약화)

    public static float EffectiveStacks(int stacks) => (float)Math.Log(1 + Math.Clamp(stacks, 0, MaxStacks), 2);

    public static IEnumerable<(StatId stat, float amount)> DecayPerTurn(int stacks, float resistance)
    {
        var factor = Strength * EffectiveStacks(stacks) * (1f - Math.Clamp(resistance, 0f, 1f));
        foreach (var (stat, perStack) in Decay)
            yield return (stat, perStack * factor);
    }

    public const float MaxResistance = 0.9f;

    // 계승 + 배운 교리의 불운 저항
    // 기본 불운 감소량 2%p 완화 (모든 판 공통)
    public const float BaseReduction = 0.02f;

    public static float TotalResistance(CultStatus cult) =>
        Math.Min(MaxResistance, BaseReduction + Legacy.Resistance + cult.Doctrines.Resistance);

    public static void ApplyTurn(CultStatus cult, int stacks, float resistance)
    {
        if (stacks <= 0) return;
        foreach (var (stat, amount) in DecayPerTurn(stacks, resistance))
            cult.AddWithRemainder(stat, -amount * GiftedV2Effects.DecayMultiplierNormal(stat, amount, "decay:misfortune"));
    }
}

// 계승: 마친 판(어떤 엔딩이든)과 업적이 쌓일수록 다음 판들이 강해진다. 지금은 불운 저항으로 나타난다.
//  - 엔딩 1번 = 1단계, 업적 20개 = 1단계
//  - 단계당 불운 저항 8% (최대 80%)
public static class Legacy
{
    public const int AchievementsPerLevel = 20;
    public const float ResistancePerLevel = 0.08f;
    public const float MaxResistance = 0.8f;

    public static int LevelFor(int endings, int achievements) => endings + achievements / AchievementsPerLevel;

    public static float ResistanceFor(int level) => Math.Min(MaxResistance, level * ResistancePerLevel);

    public static int Level => LevelFor(Endings.ReachedCount, Achievements.CountedUnlocked()); // 업적 수는 메인 업적만 세야 한다 (히든·기프티드·모드 업적 제외)
    public static float Resistance => ResistanceFor(Level);

    public static string Describe() => string.Format(Lang.Get("legacy_info"), Level, (int)Math.Round(Resistance * 100));
}
