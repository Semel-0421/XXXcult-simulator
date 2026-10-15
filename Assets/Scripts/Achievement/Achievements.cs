using System;
using UnityEngine;

// 업적 판정과 보상 지급. 게임 진행의 요소마다 Evaluate를 불러 새로 달성한 업적을 찾는다.
public static class Achievements
{
    public static event Action<Achievement> Unlocked;

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
            foreach (var achievement in AchievementCatalog.All)
            {
                if (Profile.IsUnlocked(achievement.Id) || !achievement.IsMet()) continue;
                Unlock(achievement);
                found = true;
            }
        } while (found);
    }

    static void Unlock(Achievement achievement)
    {
        Profile.MarkUnlocked(achievement.Id);
        // 기프티드는 달성 즉시 이번 판에도 지급한다 (다음 판부터는 시작할 때 지급)
        if (achievement.Reward.Kind == RewardKind.Gifted && Profile.RewardsEnabled)
            GiftedGranter.Grant(GameSession.Personal.Talents, achievement.Reward.GiftedId);
        Unlocked?.Invoke(achievement);
    }

    // 새 게임을 시작할 때: 달성한 업적의 기프티드와 스탯 초기치 보너스를 반영한다.
    public static void ApplyStartRewards(PersonalStatus personal)
    {
        if (!Profile.RewardsEnabled) return;
        foreach (var achievement in AchievementCatalog.All)
        {
            if (!Profile.IsUnlocked(achievement.Id)) continue;
            var reward = achievement.Reward;
            if (reward.Kind == RewardKind.Gifted) GiftedGranter.Grant(personal.Talents, reward.GiftedId);
            else if (reward.Kind == RewardKind.Stat) personal[reward.Stat].Add(reward.Amount);
        }
    }

    public static string RewardText(Achievement achievement)
    {
        var reward = achievement.Reward;
        switch (reward.Kind)
        {
            case RewardKind.Gifted:
                return string.Format(Lang.Get("ach_reward_gifted"), GiftedCatalog.Find(reward.GiftedId)?.Name ?? reward.GiftedId);
            case RewardKind.Stat:
                return string.Format(Lang.Get("ach_reward_stat"), Lang.Get($"pstat_{reward.Stat}"), reward.Amount);
            default:
                return "";
        }
    }
}
