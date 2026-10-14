using System;

// 행동 성공 판정: 기본 70%에 관련 스탯 비율(0~1)만큼 최대 25%가 더해지고, 기프티드 특수효과 보정이 붙는다.
public static class ActionCheck
{
    public const float BaseChance = 0.7f;
    public const float MaxBonus = 0.25f;
    const float MinChance = 0.05f, MaxChance = 0.99f;

    public static float Chance(float statRatio, float giftedBonus = 0) =>
        Math.Clamp(BaseChance + MaxBonus * Math.Clamp(statRatio, 0f, 1f) + giftedBonus, MinChance, MaxChance);

    public static bool Roll(float statRatio, Random rng, float giftedBonus = 0) => rng.NextDouble() < Chance(statRatio, giftedBonus);
}
