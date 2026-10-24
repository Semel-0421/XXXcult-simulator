using System;
using System.Collections.Generic;
using System.Linq;

// 준비된 신 모드의 업적 카운터 훅 (Docs/achievements_v2_god.csv 의 NEW:<키> 와 profile:god_*).
// 게임 코드는 한 줄로 부르기만 한다. 키 이름은 CSV 와 같고, "run" 값은 Counters 의 Run 범위(이번 회차)에 따로 쌓는다.
public static class GodCounters
{
    static readonly GodAction[] Actions = (GodAction[])Enum.GetValues(typeof(GodAction));
    static readonly GodBranch[] Branches = { GodBranch.Miracle, GodBranch.Prophecy, GodBranch.Mercy, GodBranch.Desire };
    static readonly string[] Kinds = { "heaven", "earth", "indulge", "vessel", "random" };

    static GodState endedFor; // 엔딩을 이미 기록한 판 (DueEnding/ForcedEnding 은 매 프레임 불리므로 한 번만)

    static void Both(string key, int n = 1) { Counters.Add(key, n); Counters.Add(key, n, true); }

    public static void OnRunStart()
    {
        Counters.ResetRun();
        endedFor = null;
        Profile.Count("god_runs");
    }

    public static void OnAction(GodAction action, bool success)
    {
        Both($"god_act_{action}");
        if (Actions.All(a => Counters.Get($"god_act_{a}", true) > 0)) Counters.Set("god_all_actions", 1);
    }

    public static void OnBuild(GodBuildingType type)
    {
        Both($"god_build_{type}");
        Both("god_consecrated"); // 근사: 신전 한정이 아닌 전체 축성 횟수
        OnAction(GodAction.Consecrate, true);
        if (GodBuildings.All.All(t => Counters.Get($"god_build_{t}", true) > 0)) Counters.Set("god_all_buildings", 1);
    }

    public static void OnSeized() => Both("god_seized");
    public static void OnVesselRecovery() => Both("god_vessel_recovery");
    public static void OnPoints(int gained) { if (gained > 0) Counters.Add("god_faith_points", gained); }

    public static void OnPressure(float envy, float suspicion)
    {
        foreach (var run in new[] { false, true })
        {
            Counters.Max("god_Envy", (int)envy, run);
            Counters.Max("god_Suspicion", (int)suspicion, run);
        }
    }

    // 해가 바뀔 때: 운영 연수, 시기·의혹을 10 이하로 유지한 연속 연수
    public static void OnYearEnd(GodPressure pressure)
    {
        Counters.Max("god_years", GameSession.CultTime.Years);
        Streak("Envy", pressure.Envy);
        Streak("Suspicion", pressure.Suspicion);
    }

    static void Streak(string name, float value)
    {
        var key = $"god_{name}_low_streak";
        Counters.Set(key, value <= 10 ? Counters.Get(key, true) + 1 : 0, true);
        if (Counters.Get(key, true) >= 10) Counters.Set($"god_{name}_low10y", 1);
    }

    // 노드를 익힐 때: 총 개수·가지별 개수의 최고치, 핵 완성
    public static void OnLearn(GodTree tree)
    {
        var skills = tree.Learned.Select(GodSkillCatalog.Find).Where(s => s != null).ToList();
        Counters.Max("god_nodes", skills.Count);
        foreach (var branch in Branches)
            Counters.Max($"god_branch_{branch}", skills.Count(s => s.Branch == branch));
        foreach (var core in skills.Where(s => s.IsCore)) Counters.Set($"god_core_{core.Branch}", 1);
    }

    // 이벤트 결과: 그 결과가 속한 이벤트의 종류(id 접두어)를 센다
    public static void OnEvent(Outcome outcome)
    {
        Profile.Count("god_events");
        var kind = KindOf(outcome);
        if (kind == null) return;
        Profile.Count($"god_{kind}");
        Both($"god_kind_{kind}");
        if (Kinds.All(k => Counters.Get($"god_kind_{k}", true) > 0)) Counters.Set("god_all_event_kinds", 1);
    }

    static Dictionary<Outcome, string> outcomeKind;

    static string KindOf(Outcome outcome)
    {
        if (outcomeKind == null)
        {
            outcomeKind = new Dictionary<Outcome, string>();
            foreach (var e in GodEvents.All) Index(e, KindOfId(e.Id));
        }
        if (outcomeKind.TryGetValue(outcome, out var kind)) return kind;
        foreach (var e in AdultEvents.Pool(true)) // 성인 이벤트는 탐닉으로 친다
            if (e.Question.Choices.Any(c => c.Success == outcome || c.Fail == outcome)) return "indulge";
        return null;
    }

    static void Index(CultEvent e, string kind)
    {
        foreach (var c in e.Question.Choices)
        {
            if (c.Success != null) outcomeKind[c.Success] = kind;
            if (c.Fail != null) outcomeKind[c.Fail] = kind;
        }
    }

    static string KindOfId(string id) => id.StartsWith("hv") ? "heaven" : id.StartsWith("ea") ? "earth" : id.StartsWith("vs") ? "vessel"
        : id.StartsWith("dl") ? "random" : "indulge"; // lu/gr/ap/am

    // god_ending_1~9 (CSV g012~g020) 순서: 승천 6가지를 CSV 이름 순서로(기적+예언 = god_divine, 자비+욕망 = god_human), 이어서 패배 3가지.
    // GodRules.AllEndings 순서를 쓰면 이름과 엔딩이 어긋난다. g018~g020은 패배 엔딩 3종(그릇의 붕괴·잊힌 신·해산된 교단) 업적
    static readonly string[] EndingOrder =
    {
        GodEndings.DivinePair, "god_miracle_mercy", "god_miracle_desire", "god_prophecy_mercy", "god_prophecy_desire", GodEndings.HumanPair,
        GodRules.Vessel.Id, GodRules.Forgotten.Id, GodRules.Disbanded.Id,
    };

    // 엔딩 판정이 돌려주는 값을 그대로 통과시키며, 처음 나올 때 한 번 기록한다
    public static Endings.Ending OnEnding(Endings.Ending ending)
    {
        if (ending == null || endedFor == GameSession.God) return ending;
        endedFor = GameSession.God;
        var index = Array.IndexOf(EndingOrder, ending.Id);
        if (index >= 0) Counters.Set($"god_ending_{index + 1}", 1);
        if (GodRules.IsAscension(ending)) OnAscend();
        else
        {
            Counters.Set("god_last_failed", 1);
            if (ending.Id == GodRules.Vessel.Id) Counters.Set("god_vessel_perished", 1);
        }
        return ending;
    }

    // 승천 때의 상태를 profile 값으로 남긴다 ("한 번도 안 썼다" 류는 이 값으로 판정)
    static void OnAscend()
    {
        var god = GameSession.God;
        var counts = Branches.Select(b => god.Tree.Learned.Select(GodSkillCatalog.Find).Count(s => s != null && s.Branch == b)).ToArray();
        var envy = (int)god.Pressure.Envy;
        var suspicion = (int)god.Pressure.Suspicion;
        Profile.Count("god_ascended");
        if (Counters.Get("god_last_failed") > 0) Counters.Set("god_ascend_after_fail", 1);
        Counters.Set("god_last_failed", 0);
        if (Profile.CountOf("dissolved") >= 1) Counters.Set("dissolved_then_ascended", 1); // 근사: 승천 전에 해산을 본 적이 있으면
        if (Counters.Get("god_Envy", true) >= 95) Counters.Set("god_Envy_peak95_ascend", 1);
        if (Counters.Get("god_Suspicion", true) >= 95) Counters.Set("god_Suspicion_peak95_ascend", 1);

        Counters.Set("god_ascend_years", GameSession.CultTime.Years);
        Counters.Set("god_ascend_buildings", GodBuildings.All.Sum(t => Counters.Get($"god_build_{t}", true)));
        Counters.Set("god_ascend_Wonder", Counters.Get("god_act_Wonder", true));
        Counters.Set("god_ascend_Healing", Counters.Get("god_act_Healing", true));
        // 두 핵의 갈래 밖 노드 0개로 승천 (승천에 핵 둘이 필요해 한 갈래 비중은 최대 50%라 80% 기준은 불가했다)
        var coreBranches = god.Tree.FirstCores is { } pair ? new[] { pair.first, pair.second } : Array.Empty<GodBranch>();
        var outside = Branches.Select((b, i) => coreBranches.Contains(b) ? 0 : counts[i]).Sum();
        Counters.Set("god_ascend_single_branch", coreBranches.Length == 2 && outside == 0 ? 1 : 0);
        Counters.Set("god_ascend_all_branches", counts.Min());
        Counters.Set("god_ascend_Envy", envy);
        Counters.Set("god_ascend_both", Math.Min(envy, suspicion)); // >=90 용. "<=20" 판정(g076)은 최댓값이 필요해 이 키로는 정확하지 않다
        Counters.Set("god_ascend_both_max", Math.Max(envy, suspicion));
        Counters.Set("god_ascend_points", god.Available);
        Counters.Set("god_ascend_no_vessel_recovery", Counters.Get("god_vessel_recovery", true) == 0 ? 1 : 0);
        Counters.Set("god_ascend_seized", Counters.Get("god_seized", true));
        Counters.Set("god_ascend_indulge", Counters.Get("god_kind_indulge", true));
    }
}
