using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 업적 창의 모드 탭 구분. 지금 카탈로그는 전부 일반 탭 소속, 나머지는 데이터가 생길 때까지 비어 있다.
public static class AchievementTabs
{
    public enum Tab { Normal, God, Mirror, Hidden }

    // 숨김 업적을 하나라도 달성하면 켜진다 (다른 쪽에서 이 키를 true로 저장)
    public static bool HiddenUnlocked => PlayerPrefs.GetInt("hidden_unlocked", 0) == 1;

    // 성인 업적이 생기면 여기서 AdultGate.AchievementsEnabled로 걸러낼 것
    public static IEnumerable<Achievement> Of(Tab tab) =>
        tab == Tab.Normal ? AchievementCatalog.All : Enumerable.Empty<Achievement>();
}
