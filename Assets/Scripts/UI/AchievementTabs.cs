using System.Collections.Generic;
using System.Linq;

// 업적 창의 모드 탭 구분. 일반 탭에는 기존 100개 + v2 일반, 신·거울은 v2, 히든 탭은 달성한 히든 업적만 보인다.
public static class AchievementTabs
{
    public enum Tab { Normal, God, Mirror, Hidden, Eye } // Eye는 뒤에 붙여 기존 번호(기프티드 도감 탭 등)를 유지한다

    // 히든 업적을 하나라도 달성하면 히든 탭이 생긴다 (달성 기록에서 바로 계산)
    // 황금의 눈동자 탭은 그 모드가 열렸거나 그 모드 판일 때 보인다
    public static bool EyeVisible => EyeRules.Unlocked || GameSession.EyeMode;

    public static bool HiddenUnlocked => Achievements.All.Any(a => a.Hidden && Profile.IsUnlocked(a.Id));

    public static IEnumerable<Achievement> Of(Tab tab)
    {
        switch (tab)
        {
            case Tab.Hidden: return Achievements.All.Where(a => a.Hidden && Profile.IsUnlocked(a.Id));
            case Tab.God: return Visible("god");
            case Tab.Mirror: return Visible("mirror");
            case Tab.Eye: return Visible("eye");
            default: return Visible("normal");
        }
    }

    // 성인 업적은 성인 업적 옵션이 켜진 경우에만
    static IEnumerable<Achievement> Visible(string mode) =>
        Achievements.All.Where(a => !a.Hidden && a.Mode == mode && (!a.Adult || AdultGate.AchievementsEnabled));
}
