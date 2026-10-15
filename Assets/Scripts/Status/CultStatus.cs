using System;
using System.Collections.Generic;
using System.Linq;

public class CultStatus
{
    public const int CommunionMax = 10;
    public const int StartingReputation = 500; // 명망 = 사회적 시선. 갓 생긴 교단은 중립에서 시작

    static readonly StatId[] HiddenStats = { StatId.Communion };
    public static readonly StatId[] VisibleStats = ((StatId[])Enum.GetValues(typeof(StatId))).Where(id => !IsHidden(id)).ToArray();

    readonly Dictionary<StatId, ClampedStat> stats = new()
    {
        [StatId.LeaderFaith] = new ClampedStat(),
        [StatId.FollowerFaith] = new ClampedStat(),
        [StatId.Authority] = new ClampedStat(),
        [StatId.Offerings] = new ClampedStat(),
        [StatId.Followers] = new ClampedStat(max: int.MaxValue),
        [StatId.Doctrine] = new ClampedStat(),
        [StatId.Communion] = new ClampedStat(max: CommunionMax),
        [StatId.Reputation] = new ClampedStat(StartingReputation),
    };

    public string Name { get; set; } = "";
    public DoctrineTree Doctrines { get; } = new();

    readonly RemainderBank<StatId> remainders = new();

    public ClampedStat this[StatId id] => stats[id];
    public RemainderBank<StatId> Remainders => remainders;

    // 퍼센트 보정으로 생긴 소수는 쌓아 두었다가 반올림되면 반영한다.
    // 교리 규칙도 여기서 적용: 오르는 교리는 두 배, 깎이는 신도 믿음은 교리 레벨만큼 덜 깎인다.
    public void AddWithRemainder(StatId id, float amount) => stats[id].Add(remainders.Take(id, WithDoctrineRules(id, amount)));

    float WithDoctrineRules(StatId id, float amount)
    {
        if (id == StatId.Doctrine && amount > 0) return amount * DoctrineLevel.GainMultiplier;
        if (id == StatId.FollowerFaith && amount < 0) return amount * (1f - DoctrineLevel.FaithGuard(stats[StatId.Doctrine].Value));
        return amount;
    }

    // 스탯을 0~1 비율로 환산 (이벤트 가중치·행동 성공률에 사용). 교리는 레벨/10, 신도는 1000명 기준.
    public float Ratio(StatId id)
    {
        var value = stats[id].Value;
        float ratio = id switch
        {
            StatId.Doctrine => DoctrineLevel.Of(value) / 10f,
            StatId.Followers => value / 1000f,
            _ => (float)value / stats[id].Max,
        };
        return Math.Clamp(ratio, 0f, 1f);
    }

    public static bool IsHidden(StatId id) => HiddenStats.Contains(id);
}
