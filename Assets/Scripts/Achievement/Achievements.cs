using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 업적 판정과 보상 지급. 게임 진행의 요소마다 Evaluate를 불러 새로 달성한 업적을 찾는다.
public static class Achievements
{
    public static event Action<Achievement> Unlocked;
    public static event Action Evaluated; // 시뮬레이션 전용: 판정 한 번이 끝날 때마다 (도달률 집계)

    static IReadOnlyList<Achievement> all;
    // 기존 100개 + v2(일반·신·거울·히든)
    public static IReadOnlyList<Achievement> All => all ??= AchievementCatalog.All.Concat(AchievementCatalogV2.All).ToList();

    // 총 업적 수(achv 조건·진행 표시)에 셀 대상: 히든·성인 제외. mode가 null이면 전체.
    public static IEnumerable<Achievement> Counted => All.Where(a => !a.Hidden && !a.Adult);
    public static int CountedUnlocked(string mode = null) =>
        Counted.Count(a => (mode == null || a.Mode == mode) && Profile.IsUnlocked(a.Id));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EnableInPlayer() => Profile.Enabled = true;

    // 새로 달성한 업적을 모두 찾는다. 업적 수 조건(achv)이 연쇄할 수 있어 더 나오지 않을 때까지 반복한다.
    public static void Evaluate()
    {
        if (!Profile.Enabled) return;
        bool found;
        do
        {
            found = false;
            foreach (var achievement in All)
            {
                if (Profile.IsUnlocked(achievement.Id)) continue;
                if (achievement.Adult && !AdultGate.AchievementsEnabled) continue; // 꺼진 동안은 해금하지 않고 카운터만 쌓임, 켜면 다음 판정에 바로 해금
                if (!achievement.IsMet()) continue;
                Unlock(achievement);
                found = true;
            }
        } while (found);
        Evaluated?.Invoke();
        Counters.Save();
    }

    static void Unlock(Achievement achievement)
    {
        Profile.MarkUnlocked(achievement.Id);
        // 기프티드는 달성 즉시 이번 판에도 지급한다 (다음 판부터는 시작할 때 지급). v2 id는 지급 없이 달성 자체로 보유(OwnsGiftedV2) → 바로 효과가 난다
        if (achievement.Reward.Kind == RewardKind.Gifted && Profile.RewardsEnabled && GiftedCatalog.Find(achievement.Reward.GiftedId) != null)
            GiftedGranter.Grant(GameSession.Personal.Talents, achievement.Reward.GiftedId);
        Unlocked?.Invoke(achievement);
    }

    // 새 게임을 시작할 때: 달성한 업적의 기프티드와 스탯 초기치 보너스를 반영한다.
    public static void ApplyStartRewards(PersonalStatus personal)
    {
        if (!Profile.RewardsEnabled) return;
        foreach (var achievement in All)
        {
            if (!Profile.IsUnlocked(achievement.Id)) continue;
            var reward = achievement.Reward;
            // v2 기프티드 id(v2n001...)는 TalentList에 넣지 않는다: 연결 업적 달성으로 보유하고 GiftedV2Effects 훅으로 효과가 난다
            if (reward.Kind == RewardKind.Gifted) { if (GiftedCatalog.Find(reward.GiftedId) != null) GiftedGranter.Grant(personal.Talents, reward.GiftedId); }
            else if (reward.Kind == RewardKind.Stat) personal[reward.Stat].Add(reward.Amount);
        }
        GiftedV2Effects.OnRunStart();
    }

    // v2 기프티드 보유: 연결 업적을 프로필에서 달성했으면 보유 (회차를 넘어 유지). 보상 끔·에디터 테스트(Profile 꺼짐)면 아무것도 보유하지 않는다
    public static bool V2OwnershipOn => Profile.Enabled && Profile.RewardsEnabled;
    // 시뮬레이션 전용: true면 v2 기프티드 전부 보유, false면 하나도 없음, null(기본)이면 위 규칙
    public static bool? ForceGiftedV2;
    public static bool OwnsGiftedV2(GiftedV2Catalog.Entry entry) => V2OwnershipOn && (ForceGiftedV2 ?? Profile.IsUnlocked(entry.LinkedAchievement));

    public static string RewardText(Achievement achievement)
    {
        var reward = achievement.Reward;
        switch (reward.Kind)
        {
            case RewardKind.Gifted:
                return string.Format(Lang.Get("ach_reward_gifted"),
                    GiftedCatalog.Find(reward.GiftedId)?.Name ?? (GiftedV2Catalog.Find(reward.GiftedId) is { } v2 ? Lang.Pick(v2.Name) : reward.GiftedId));
            case RewardKind.Stat:
                return string.Format(Lang.Get("ach_reward_stat"), Lang.Get($"pstat_{reward.Stat}"), reward.Amount);
            default:
                return "";
        }
    }
}
