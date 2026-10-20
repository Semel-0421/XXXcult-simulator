using UnityEngine;
﻿// 명망 = 교단을 바라보는 사회적 시선. 낮으면 지역 사회·타 종교·국가의 견제를 받다 해체되고, 높으면 공인된 종교로 대우받는다.
public enum Standing { Dissolved, Persecuted, Watched, Neutral, Respected, Recognized }

public static class SocialStanding
{
    public const int PersecutedBelow = 150, WatchedBelow = 300, RespectedFrom = 700, RecognizedFrom = 900;

    public static Standing Of(int reputation) => reputation switch
    {
        <= 0 => Standing.Dissolved,
        < PersecutedBelow => Standing.Persecuted,
        < WatchedBelow => Standing.Watched,
        < RespectedFrom => Standing.Neutral,
        < RecognizedFrom => Standing.Respected,
        _ => Standing.Recognized,
    };

    public static Standing Of(CultStatus cult) => Of(Mathf.FloorToInt(CultScore.Reputation(cult))); // 신도→명망 연결을 반영한 명망 점수

    // 교단 턴마다 붙는 효과. 높은 명망은 주목받는 만큼 조금씩 깎인다.
    public static CultEffect[] TurnEffects(Standing standing) => standing switch
    {
        Standing.Persecuted => new[] { new CultEffect(StatId.FollowerFaith, -6), new CultEffect(StatId.Offerings, -6) },
        Standing.Watched => new[] { new CultEffect(StatId.Offerings, -3) },
        Standing.Respected => new[] { new CultEffect(StatId.Offerings, 5), new CultEffect(StatId.Reputation, -1) },
        Standing.Recognized => new[] { new CultEffect(StatId.Offerings, 10), new CultEffect(StatId.Reputation, -3) },
        _ => System.Array.Empty<CultEffect>(),
    };

    // 입교: 해마다 명망 400을 기준으로 100마다 1명씩 들어오거나(+) 떠난다(-)
    public const int MaxRecruitPerYear = 10, RecruitPivot = 400, RecruitStep = 100, BaseRecruit = 2, RecruitPerStep = 2;

    public static int Recruits(int reputation) =>
        Mathf.Clamp(BaseRecruit + (int)Mathf.Sign(reputation - RecruitPivot) * Mathf.CeilToInt(Mathf.Abs(reputation - RecruitPivot) / (float)RecruitStep) * RecruitPerStep, -MaxRecruitPerYear, MaxRecruitPerYear); // 기본 +2명, 명망 400 기준 100마다 ±2명 (0 구간 없음)

    public static void ApplyTurn(CultStatus cult)
    {
        foreach (var effect in TurnEffects(Of(cult)))
            cult[effect.Stat].Add(effect.Amount);
    }

    // 연 1회(교단 1년차마다) 입교를 더한다
    public static void YearEnd(CultStatus cult)
    {
        var before = cult[StatId.Followers].Value;
        cult[StatId.Followers].Add(Recruits(cult[StatId.Reputation].Value));
        MirrorCounters.Recruits(cult[StatId.Followers].Value - before);
    }

}
