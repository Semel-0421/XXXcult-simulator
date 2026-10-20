using System;

// 일반 모드 v2 기프티드용 추가 훅·칸 (GiftedV2Effects.cs와 충돌을 피하려고 partial로 분리). 보유 없음 = 모두 무변화, 난수 미사용.
public sealed partial class EffectDef
{
    public float? MisfortuneGainReduceNormal;      // 불운 한 겹이 쌓이는 것을 이 % 확률로 막음 (v2n007)
    public float? DoctrineCostReducePercentNormal; // 교리에 쓴 포인트 합계에서 이 %만큼(내림) 돌려받음 (v2n025)
    public Func<int, int> DoctrinePointBonusNormal; // 교단 연수 -> 추가 교리 포인트 (v2n036)
    public Func<Choice> RescueChoiceNormal;        // 위기(부정적) 이벤트에 덧붙일 선택지, 없으면 null (v2n004)
    public float? GiftedGrantBonusNormal;          // 기프티드를 얻을 때 하나 더 얻을 확률 (v2n006)
}

public static partial class GiftedV2Effects
{
    // GameSession.AddMisfortune: 막으면 true
    public static bool MisfortuneGainBlockedNormal()
    {
        float percent = 0;
        foreach (var d in Active()) percent += d.MisfortuneGainReduceNormal ?? 0;
        return percent > 0 && GameSession.Rng.NextDouble() < percent / 100.0;
    }

    // 쇠퇴(불운·시선·이탈)로 깎이는 양의 배율. 출처는 Other. amount는 크기만 본다(0이면 1)
    public static float DecayMultiplierNormal(StatId stat, float amount, string key) =>
        amount == 0f ? 1f : StatMultiplier(new StatChange(ChangeSource.Other, key, stat, null, -1));

    // DoctrineTree.SpentPoints
    public static int DoctrineSpentAdjustNormal(int spent)
    {
        float percent = 0;
        foreach (var d in Active()) percent += d.DoctrineCostReducePercentNormal ?? 0;
        return percent <= 0 ? spent : spent - (int)Math.Floor(spent * Math.Min(percent, 100f) / 100f);
    }

    // DoctrinePoints.Earned
    public static int DoctrinePointBonusNormal(int cultYears)
    {
        int bonus = 0;
        foreach (var d in Active()) if (d.DoctrinePointBonusNormal != null) bonus += d.DoctrinePointBonusNormal(cultYears);
        return bonus;
    }

    // CultFlow: 위기 이벤트 질문에 구원 선택지를 덧붙인다
    public static Question WithRescueChoiceNormal(Question q)
    {
        if (q == null) return q;
        Choice extra = null;
        foreach (var d in Active().ToArray())
            if (d.RescueChoiceNormal != null && (extra = d.RescueChoiceNormal()) != null) break;
        if (extra == null) return q;
        var choices = new Choice[q.Choices.Length + 1];
        Array.Copy(q.Choices, choices, q.Choices.Length);
        choices[^1] = extra;
        return new Question(q.Text, choices);
    }

    // Turns.Answer: 기프티드를 얻은 직후 하나 더 얻는가
    public static bool GiftedBonusRollNormal()
    {
        float percent = 0;
        foreach (var d in Active()) percent += d.GiftedGrantBonusNormal ?? 0;
        return percent > 0 && GameSession.Rng.NextDouble() < percent / 100.0;
    }
}
