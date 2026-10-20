using System;
using System.Collections.Generic;

// 휴식: 개인 스탯이 하한(0)에 이르면 교주가 1년(4분기) 쉬면서 그 수치를 회복한다.
//  - 쉬는 동안 매 분기: 0이 된 스탯은 +Recover, 휴식 범위의 나머지 개인 스탯은 -Drain, 보이는 교단 스탯은 -CultDrain.
//  - 쉬는 동안에는 다른 이벤트가 발동하지 않는다 (행동·이벤트 없이 시간만 흐른다).
//  - 휴식이 끝났을 때 0인 스탯이 또 있으면(휴식 중 새로 0이 된 것 포함) 다시 휴식한다. 모두 0이 아닐 때까지 반복.
//  - 여러 스탯이 동시에 0이면 한 번에 함께 휴식한다.
// 휴식 범위: 건강군(체력·정신력·신앙심·평판·재산)만. 욕망류·덕목류·히든 스탯(기적)은 범위 밖이다.
// 시작부터 0인 스탯은 하한에 이른 것이 아니다: 한 번이라도 0을 넘은 적이 있는 스탯이 0으로 돌아왔을 때만 센다.
public static class Rest
{
    public const int Turns = 4;       // 1년
    public const int Recover = 3;     // 휴식 대상 스탯이 분기마다 오르는 양
    public const int Drain = 1;       // 그 밖의 개인 스탯이 분기마다 깎이는 양
    public const int CultDrain = 3;   // 교단 스탯이 분기마다 깎이는 양

    static readonly PersonalStatId[] All = (PersonalStatId[])Enum.GetValues(typeof(PersonalStatId));
    static readonly StatId[] CultStats = (StatId[])Enum.GetValues(typeof(StatId));

    // 휴식 범위에 드는 개인 스탯인가: 건강군만 (휴식 발동도, 휴식 중 감소도 이 스탯들만)
    public static bool InScope(PersonalStatId id) =>
        id is PersonalStatId.Health or PersonalStatId.Mentality or PersonalStatId.Piety or PersonalStatId.Repute or PersonalStatId.Wealth;

    // 0을 넘은 적이 있는 스탯의 비트마스크를 갱신해 돌려준다
    public static int UpdateRaised(PersonalStatus personal, int mask)
    {
        foreach (var id in All)
            if (personal[id].Value > 0) mask |= 1 << (int)id;
        return mask;
    }

    // 하한에 이른 휴식 범위의 스탯
    public static List<PersonalStatId> ZeroStats(PersonalStatus personal, int raisedMask)
    {
        var zero = new List<PersonalStatId>();
        foreach (var id in All)
        {
            if (!InScope(id)) continue;
            var stat = personal[id];
            if (stat.Value <= stat.Min && (raisedMask & (1 << (int)id)) != 0) zero.Add(id);
        }
        return zero;
    }

    // 게임 진행 중에 쓰는 판정: GameSession의 기록을 갱신하며 확인한다
    public static List<PersonalStatId> ZeroStats(PersonalStatus personal)
    {
        GameSession.MergePersonalRaised(UpdateRaised(personal, 0));
        return ZeroStats(personal, GameSession.PersonalRaisedMask);
    }

    public static bool NeedsRest(PersonalStatus personal) => ZeroStats(personal).Count > 0;

    // 휴식 한 분기: 대상은 +Recover, 범위의 나머지 개인 스탯은 -Drain, 보이는 교단 스탯은 -CultDrain
    public static void ApplyTurn(PersonalStatus personal, CultStatus cult, ICollection<PersonalStatId> resting)
    {
        foreach (var id in All)
        {
            if (resting.Contains(id)) { var m = GiftedV2Mirror.RestMultiplier(); if (m == 1f) personal[id].Add(Recover); else personal.AddWithRemainder(id, Recover * m); }
            else if (InScope(id)) personal[id].Add(-Drain);
        }
        foreach (var id in CultStats)
            if (!CultStatus.IsHidden(id)) cult.AddWithRemainder(id, -CultDrain);
    }
}
