using System;
using System.Collections.Generic;
using System.Linq;

public class CultStatus
{
    public const int CommunionMax = 10;
    public const int StartingReputation = 750;

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

    public ClampedStat this[StatId id] => stats[id];

    public static bool IsHidden(StatId id) => HiddenStats.Contains(id);
}
