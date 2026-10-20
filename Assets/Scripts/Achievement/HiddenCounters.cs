using System;
using System.Linq;

// 히든 업적용 카운터 훅 (키는 AchievementCatalogV2 머리말 / Docs/achievements_v2_hidden.csv). 모든 모드 공통, Profile 범위.
public static class HiddenCounters
{
    // 세이브 복원 중에는 기프티드 누적(gifted_total)을 올리지 않는다 (GameSession.Restore가 다시 지급하므로)
    public static bool Muted;

    // GameSession.AdvanceTurn: 욕망·미덕 평균의 최고치. 정수는 내림 (60 이상은 실제 평균 60 이상일 때만)
    public static void Turn()
    {
        var p = GameSession.Personal;
        int d = (int)CultEvent.TraitAverage(p, CultEvent.MoodTrait.Desire), v = (int)CultEvent.TraitAverage(p, CultEvent.MoodTrait.Virtue);
        Counters.Max("desire_avg_peak", d);
        Counters.Max("virtue_avg_peak", v);
        Counters.Max("dual_avg_peak", Math.Min(d, v));
    }

    // TalentList.Add (NormalCounters.OnGifted 다음): ownedCount = 지금 보유 수
    public static void OnGifted(int ownedCount)
    {
        if (!Muted) Counters.Add("gifted_total");
        Counters.Max("gifted_run_peak", ownedCount);
        Counters.Max("gifted_kinds_seen", Profile.CountOf("gifted_seen"));
    }

    // Progress.Reach (Endings.Reach 이후): 일반·신·거울 모든 엔딩
    public static void OnEnding(Endings.Ending ending)
    {
        Turn();
        Counters.Add("ending_total");

        // shortcut: "클리어" = 신은 승천, 거울은 붕괴(흡수·신도 이탈) 아님, 일반은 해산·요절 아님
        bool clear = GameSession.GodMode ? GodRules.IsAscension(ending)
            : GameSession.MirrorMode ? ending.Id is not ("inv_end_absorbed" or "inv_end_followers")
            : ending.Id is not (Endings.Dissolved or Endings.DeathYoung);
        if (clear)
        {
            var mode = GameSession.GodMode ? "god" : GameSession.MirrorMode ? "mirror" : "normal";
            Counters.Set("hidden_clear_" + mode, 1);
            Counters.Max("hidden_clear_modes", Counters.Get("hidden_clear_normal") + Counters.Get("hidden_clear_god") + Counters.Get("hidden_clear_mirror"));
            // 판정 실패는 GameSession 카운터(저장됨)로 센다: 행동 실패 + 선택지 실패
            if (GameSession.CountOf("act_fail") + GameSession.CountOf("choice_fail") == 0) Counters.Set("clear_no_fail", 1);
        }

        bool Seen(DoctrineBranch b, EndingKind k) => Endings.All.Any(e => e.Final != null && e.Final.Branch == b && e.Kind == k && Endings.Seen(e.Id));
        if (Enumerable.Range(1, 8).All(i => Seen((DoctrineBranch)i, EndingKind.Good) && Seen((DoctrineBranch)i, EndingKind.Bad)))
            Counters.Set("both_endings_all8", 1);
        if (Endings.All.Concat(GodRules.AllEndings).Concat(MirrorEndings.All).All(e => Endings.Seen(e.Id)))
            Counters.Set("all_endings_seen", 1);
    }
}
