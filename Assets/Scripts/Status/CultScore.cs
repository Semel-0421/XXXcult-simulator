using UnityEngine;

// 교단 점수: 신도 → 명망 → 헌금 순서로 한 번만 더한다 (되돌아오는 연결 없음, 순환 계산 없음).
// 각 연결의 보너스는 받는 쪽 기본 값의 최대 50%로 제한한다.
public static class CultScore
{
    const float FollowerToReputation = 5f, ReputationToOfferings = 0.5f, MaxShare = 0.5f;

    // 명망 점수 = 명망 + min(명망의 50%, 신도 × 5)
    public static float Reputation(CultStatus cult)
    {
        float baseRep = cult[StatId.Reputation].Value;
        return baseRep + Mathf.Min(baseRep * MaxShare, cult[StatId.Followers].Value * FollowerToReputation * GiftedV2Mirror.FollowerReputationRate());
    }

    // 헌금 점수 = 헌금 + min(헌금의 50%, 명망 점수 × 0.5)  (명망 점수는 위에서 한 번만 계산한 값)
    public static float Offerings(CultStatus cult)
    {
        float baseOff = cult[StatId.Offerings].Value;
        return baseOff + Mathf.Min(baseOff * MaxShare, Reputation(cult) * ReputationToOfferings * GiftedV2Mirror.ReputationOfferingsRate());
    }
}
