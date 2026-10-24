using System;
using System.Collections.Generic;

public enum RewardKind { None, Gifted, Stat }

// 업적 달성 보상: 기프티드 한 개, 또는 다음 판부터 적용되는 개인 스탯 초기치 보너스.
public readonly struct AchievementReward
{
    public RewardKind Kind { get; }
    public string GiftedId { get; }
    public PersonalStatId Stat { get; }
    public int Amount { get; }

    AchievementReward(RewardKind kind, string giftedId, PersonalStatId stat, int amount)
    {
        Kind = kind; GiftedId = giftedId; Stat = stat; Amount = amount;
    }

    public static AchievementReward None => default;
    public static AchievementReward Gifted(string id) => new(RewardKind.Gifted, id, default, 0);
    public static AchievementReward StatBonus(PersonalStatId stat, int amount) => new(RewardKind.Stat, null, stat, amount);
}

public class Achievement
{
    public string Id { get; }
    readonly string name, description;
    public string Name => Lang.Pick(name);
    public string Description => Lang.Pick(description);
    public string ConditionText { get; }
    public AchievementReward Reward { get; }
    public AchievementCondition Condition { get; }
    public IReadOnlyList<string> CounterKeys { get; }
    public string Mode { get; }   // normal / god / mirror / hidden
    public int Tier { get; }      // 난이도 표시 1~3 (기존 업적은 0)
    public bool Hidden { get; }   // 히든 업적: 달성 전에는 목록에 안 보임, 총 업적 수에서 뺌
    public bool Adult { get; }    // 성인 업적 옵션이 켜져 있어야 나타남, 총 업적 수에서 뺌

    public Achievement(string id, string name, string description, string condition, AchievementReward reward,
        string mode = "normal", int tier = 0, bool hidden = false, bool adult = false)
    {
        Mode = mode; Tier = tier; Hidden = hidden; Adult = adult;
        Id = id; this.name = name; this.description = description; ConditionText = condition; Reward = reward;
        Condition = AchievementCondition.Parse(condition);
        CounterKeys = AchievementCondition.ExtractCounterKeys(condition);
    }

    public bool IsMet() => Condition.IsMet();
}
