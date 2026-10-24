using System;

// 선택지·행동 효과에 보유 기프티드의 퍼센트 보정을 더해 개인/교단 스탯에 반영한다.
// 보정은 항상 유리한 방향: +%는 증가를 키우고 감소를 줄인다(-%는 반대). 준비된 신 모드면 권능 보정도 곱한다.
public static class GiftedEffects
{
    // 매 선택마다 호출되므로 TalentList 캐시를 사용 (이중 루프 제거)
    public static float TotalPercent(TalentList gifted, PersonalStatId stat) =>
        gifted.GetPersonalPercent(stat);

    public static float TotalPercent(TalentList gifted, StatId stat) =>
        gifted.GetCultPercent(stat);

    public static float Adjust(int amount, float percent) => amount + Math.Abs(amount) * percent / 100f;

    public static void Apply(PersonalStatus personal, PersonalStatId stat, int amount) =>
        personal.AddWithRemainder(stat, Adjust(amount, TotalPercent(personal.Talents, stat)) * GodRules.Scale(stat, amount) * GiftedV2Effects.StatMultiplier(stat, amount));

    public static void Apply(CultStatus cult, TalentList gifted, StatId stat, int amount) =>
        cult.AddWithRemainder(stat, Adjust(amount, TotalPercent(gifted, stat)) * GodRules.Scale(stat, amount) * GiftedV2Effects.StatMultiplier(stat, amount));
}
