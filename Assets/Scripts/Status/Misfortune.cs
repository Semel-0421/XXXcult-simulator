using System;
using System.Collections.Generic;

// 불운: 부정적 이벤트가 닥칠 때마다 한 겹씩 쌓이고, 쌓인 만큼 매 턴 교단 스탯이 깎인다. 판이 끝날 때까지 사라지지 않는다.
// 계승(Legacy)의 불운 저항이 깎이는 양을 줄여 준다.
public static class Misfortune
{
    public const int MaxStacks = 10;

    // 불운 한 겹당 매 턴 깎이는 양 (소수는 쌓아 두었다가 반영)
    static readonly (StatId stat, float perStack)[] Decay =
    {
        (StatId.Reputation, 3f),
        (StatId.FollowerFaith, 4f),
        (StatId.LeaderFaith, 3f),
        (StatId.Authority, 3f),
        (StatId.Offerings, 4f),
        (StatId.Doctrine, 2f),
        (StatId.Followers, 0.5f),
    };

    public static IEnumerable<(StatId stat, float amount)> DecayPerTurn(int stacks, float resistance)
    {
        var factor = Math.Clamp(stacks, 0, MaxStacks) * (1f - Math.Clamp(resistance, 0f, 1f));
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
            cult.AddWithRemainder(stat, -amount);
    }
}

// 계승: 게임 오버(교단 해체)와 업적이 쌓일수록 다음 판들이 강해진다. 지금은 불운 저항으로 나타난다.
//  - 교단 해체 1번 = 1단계, 업적 10개 = 1단계
//  - 단계당 불운 저항 8% (최대 80%)
public static class Legacy
{
    public const int AchievementsPerLevel = 10;
    public const float ResistancePerLevel = 0.08f;
    public const float MaxResistance = 0.8f;

    public static int LevelFor(int dissolutions, int achievements) => dissolutions + achievements / AchievementsPerLevel;

    public static float ResistanceFor(int level) => Math.Min(MaxResistance, level * ResistancePerLevel);

    public static int Level => LevelFor(Profile.CountOf("dissolved"), Profile.UnlockedCount);
    public static float Resistance => ResistanceFor(Level);

    public static string Describe() => string.Format(Lang.Get("legacy_info"), Level, (int)Math.Round(Resistance * 100));
}
