using System;

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

    public Achievement(string id, string name, string description, string condition, AchievementReward reward)
    {
        Id = id; this.name = name; this.description = description; ConditionText = condition; Reward = reward;
        Condition = AchievementCondition.Parse(condition);
    }

    public bool IsMet() => Condition.IsMet();
}
