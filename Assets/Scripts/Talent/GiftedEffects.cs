using System;

// 선택지 효과에 보유 기프티드의 퍼센트 보정을 더해 개인 스탯에 반영한다.
// 보정은 항상 유리한 방향: +%는 증가를 키우고 감소를 줄인다(-%는 반대).
public static class GiftedEffects
{
    // 매 선택마다 호출되므로 LINQ 없이 합산한다
    public static float TotalPercent(TalentList gifted, PersonalStatId stat)
    {
        float total = 0;
        foreach (var g in gifted.Owned)
            foreach (var m in g.Modifiers)
                if (m.PersonalStat == stat) total += m.Percent;
        return total;
    }

    public static float Adjust(int amount, float percent) => amount + Math.Abs(amount) * percent / 100f;

    public static void Apply(PersonalStatus personal, PersonalStatId stat, int amount) =>
        personal.AddWithRemainder(stat, Adjust(amount, TotalPercent(personal.Talents, stat)));
}
