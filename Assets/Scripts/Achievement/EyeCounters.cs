using System.Linq;

// 황금의 눈동자 모드의 업적 카운터 훅 (Docs/achievements_v2_eye.csv 의 NEW:<키> 와 profile:eye_*).
// 게임 코드는 한 줄로 부르기만 한다. 판 기준 값은 Counters의 Run 범위(perRun), 최고 기록은 누적 범위에 Max로 둔다.
public static class EyeCounters
{
    static bool On => GameSession.EyeMode;

    public static void OnSector(EyeSector sector)
    {
        if (!On) return;
        Profile.Count($"eye_start_{Key(sector)}");
    }

    static string Key(EyeSector sector) => sector.ToString().ToLowerInvariant();

    public static void OnLearn(AeonTree tree)
    {
        if (!On) return;
        var skills = tree.Learned.Select(AeonSkillCatalog.Find).Where(s => s != null).ToList();
        Counters.Max("eye_nodes", skills.Count);
        Counters.Max("eye_good", skills.Count(s => s.Kind == AeonKind.Good));
        Counters.Max("eye_evil", skills.Count(s => s.Kind == AeonKind.Evil));
        Counters.Max("eye_gnosis_count", EyeRules.Sectors.Count(s => tree.IsLearned(AeonSkillCatalog.Stage(s, AeonSkill.GnosisLevel))));
        if (EyeRules.Sectors.All(s => skills.Any(k => k.Sector == s))) Counters.Set("eye_all_sectors", 1);
        foreach (var sector in EyeRules.Sectors)
        {
            if (tree.IsLearned(AeonSkillCatalog.Stage(sector, AeonSkill.GnosisLevel))) Counters.Set($"eye_gnosis_{Key(sector)}", 1);
            if (tree.IsLearned(AeonSkillCatalog.Stage(sector, AeonSkill.ThresholdLevel))) Counters.Set($"eye_threshold_{Key(sector)}", 1);
        }
    }

    public static void OnAction(GodAction action)
    {
        if (!On) return;
        Profile.Count($"eye_act_{action}");
    }

    public static void OnBuild()
    {
        if (!On) return;
        Profile.Count("eye_build_total");
        Profile.Count($"eye_act_{GodAction.Consecrate}");
    }

    public static void OnEvent()
    {
        if (!On) return;
        Profile.Count("eye_events");
    }

    // kind: cult / body / half / rest
    public static void OnCalamity(string kind)
    {
        if (!On) return;
        Profile.Count("eye_calamities");
        Counters.Add("eye_run_calamities", 1, perRun: true);
        Profile.Count(kind == "rest" ? "eye_rest_calamities" : $"eye_give_{kind}");
    }

    public static void OnQuarter()
    {
        if (!On || !GameSession.God.Demiurge.Copied) return;
        Counters.Max("eye_dominance_peak", (int)(EyeRules.Dominance * 100), perRun: true);
        Counters.Max("eye_years", GameSession.CultTime.Years);
        var d = GameSession.God.Demiurge;
        if (DemiurgeState.CultStats.Any(id => d[id] >= DemiurgeState.CultCap) || DemiurgeState.PersonalStats.Any(id => d[id] >= DemiurgeState.PersonalCap))
            Counters.Set("eye_demiurge_max", 1);
    }

    // 엔딩: 승천 순간의 기록을 판 범위에 남긴다 (Progress.Reach에서 Endings.Reach 뒤에 부른다)
    public static void OnEnding(Endings.Ending ending)
    {
        if (!On || EyeEndings.Find(ending.Id) == null) return;
        var bad = ending.Kind == EndingKind.Bad;
        Profile.Count("eye_ascended");
        Profile.Count(bad ? "eye_lost" : "eye_usurped");
        Counters.Max("eye_endings_seen", EyeEndings.All.Count(e => Endings.Seen(e.Id)));
        var god = GameSession.God;
        Counters.Set("eye_ascend_calamities", god.Demiurge.Calamities, perRun: true);
        Counters.Set("eye_ascend_dominance", (int)(EyeRules.Dominance * 100), perRun: true);
        Counters.Set("eye_ascend_good", bad ? 0 : 1, perRun: true);
        Counters.Set("eye_ascend_years", GameSession.CultTime.Years, perRun: true);
        var sector = god.Aeons.AscensionSector;
        var single = god.Aeons.Learned.Select(AeonSkillCatalog.Find).All(s => s == null || s.Sector == sector);
        Counters.Set("eye_ascend_single_sector", single && !bad ? 1 : 0, perRun: true);
    }
}
