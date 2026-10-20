using System.Collections.Generic;
using System.Linq;

// 배경음악·효과음 파일 이름 규칙. 음악은 Assets/Resources/Audio/Music/{키}, 효과음은 Audio/Sfx/{키} (ogg|wav|mp3).
// 곡마다 어떤 분위기인지는 Docs/audio_design.md.
//   menu / life(교단 창설 전) / tier_{명망 단계} / tier_Dissolved(해체 결말)
//   god_main / god_heaven / god_earth / god_clash(두 압박이 같을 때) / god_ascend(승천)
//   ending_glory / ending_ruin / ending_death
public static class MusicKeys
{
    public const string Menu = "menu", Life = "life";
    public const string GodMain = "god_main", GodHeaven = "god_heaven", GodEarth = "god_earth", GodClash = "god_clash", GodAscend = "god_ascend";
    public const string EndingGlory = "ending_glory", EndingRuin = "ending_ruin", EndingDeath = "ending_death";
    public const int GodPressureStage = 3; // 신계·지상 중 높은 쪽이 이 단계 이상이면 압박 곡으로 바꾼다

    public static string ForStanding(Standing standing) => $"tier_{standing}";

    // 압박이 더 센 쪽의 곡, 같으면 두 분위기를 함께 담은 곡
    public static string ForGod(int heavenStage, int earthStage)
    {
        if (System.Math.Max(heavenStage, earthStage) < GodPressureStage) return GodMain;
        if (heavenStage == earthStage) return GodClash;
        return heavenStage > earthStage ? GodHeaven : GodEarth;
    }

    public static string ForEnding(Endings.Ending ending)
    {
        if (GodRules.IsAscension(ending)) return GodAscend;
        if (Progress.IsDeath(ending)) return EndingDeath;
        if (ending.Id == Endings.Dissolved) return "tier_Dissolved";
        if (ending.Kind == EndingKind.Good || (ending.IsExtreme && ending.Id.EndsWith("max"))) return EndingGlory;
        return EndingRuin; // 나쁜 최종 스킬·극단(바닥)·신 모드의 잊힘·해산
    }

    // 해당 키의 파일이 없을 때 대신 찾아볼 키 (앞쪽부터 시도)
    public static string[] Candidates(string key) => key switch
    {
        "tier_Dissolved" => new[] { key, EndingRuin, "tier_Persecuted" },
        GodClash => new[] { key, GodHeaven, GodMain },
        GodHeaven or GodEarth => new[] { key, GodMain },
        GodAscend => new[] { key, EndingGlory },
        Life => new[] { key, Menu },
        _ => new[] { key },
    };

    static HashSet<string> adultIds, negativeIds;

    // 이벤트 창이 뜰 때: 성인 이벤트 / 부정적·압박 이벤트 / 그 밖
    public static string EventSfx(string eventId)
    {
        adultIds ??= AdultEvents.All.Select(e => e.Id).ToHashSet();
        negativeIds ??= CultEvents.All.Concat(GodEvents.All).Concat(MirrorEvents.All).Where(e => e.IsNegative).Select(e => e.Id).ToHashSet();
        if (adultIds.Contains(eventId)) return "adult_open";
        return negativeIds.Contains(eventId) ? "event_bad" : "event_open";
    }
}
