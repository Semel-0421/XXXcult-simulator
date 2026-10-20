using System;
using System.Linq;

// 거울 세계 업적용 카운터 훅 (키는 Docs/achievements_v2_mirror.csv 의 NEW:/profile:mirror_*).
// 게임 쪽은 이 클래스의 한 줄 호출만 한다. 거울 판이 아니면 아무것도 세지 않는다.
// Peak 류(신도·명망·헌금·불운·연수)는 Profile+Run 양쪽에 최댓값을 올리고, 누적류(실패·이벤트·회차)는 Profile에만 쌓는다.
public static class MirrorCounters
{
    static readonly DoctrineBranch[] Branches =
    {
        DoctrineBranch.Lust, DoctrineBranch.Greed, DoctrineBranch.Appetite, DoctrineBranch.Ambition,
        DoctrineBranch.Temperance, DoctrineBranch.Charity, DoctrineBranch.Patience, DoctrineBranch.Integrity,
    };

    const int GraceTurns = 16;

    static bool Active => GameSession.MirrorMode;
    static void Peak(string key, int value) { Counters.Max(key, value); Counters.Max(key, value, true); }
    static string Key(DoctrineBranch b) => "mirror_doctrine_" + b.ToString().ToLowerInvariant();

    // 거울 판 시작 (GameSession.SetMirrorMode(true))
    public static void RunStart()
    {
        Counters.Add("mirror_runs");
        if (Counters.Get("mirror_gap") == 0) Counters.Set("mirror_gap", 99); // "gap<=5" 조건이 시작값 0으로 참이 되지 않게
    }

    // 교단 턴이 지난 뒤 (GameSession.AdvanceTurn, 거울 판 · 창설 후)
    public static void Turn()
    {
        if (!Active) return;
        var cult = GameSession.Cult;
        int followers = cult[StatId.Followers].Value, rep = cult[StatId.Reputation].Value, offer = cult[StatId.Offerings].Value;
        var time = GameSession.CultTime;
        Peak("mirror_followers", followers);
        Peak("mirror_Reputation", rep);
        Peak("mirror_Offerings", offer);
        Peak("mirror_chain_all", Math.Min(followers, Math.Min(rep, offer)));
        Peak("mirror_cultyears", time.Years);
        Peak("mirror_doctrine_points", DoctrinePoints.Earned(cult, time, Achievements.CountedUnlocked()));

        if (followers <= 5 && time.Turns > GraceTurns) Counters.Set("mirror_low_followers", 1, true); // 시작 직후(신도 0명에서 출발)는 '바닥'으로 치지 않는다
        if (Counters.Get("mirror_low_followers", true) > 0)
        {
            if (followers >= 500) Counters.Set("mirror_followers_5_to_500", 1);
            if (time.Years >= 10 && followers > 5) Counters.Set("mirror_followers_low_recover", 1);
        }

        if (time.Turns <= GraceTurns)
        {
            Peak("mirror_grace_followers", followers);
            if (time.Turns == GraceTurns && Counters.Get("mirror_early_fails", true) == 0) Counters.Set("mirror_grace_no_fail", 1);
        }

        // 욕망/미덕 우세가 뒤집힌 횟수
        int sign = LeadSign();
        if (sign != 0)
        {
            int last = Counters.Get("mirror_lead_sign", true);
            if (last != 0 && last != sign) Counters.Add("mirror_lead_swaps", 1, true);
            Counters.Set("mirror_lead_sign", sign, true);
        }

        if (time.Quarter != 0 || time.Years <= 0) return; // 이하 교단 해가 바뀔 때만
        if (GameSession.Misfortune <= 1) Counters.Add("mirror_misfortune_low_years");
        if (GameSession.Misfortune >= 6) Counters.Add("mirror_misfortune_high_years");
        if (Math.Abs(rep - 400) <= 50) Counters.Add("mirror_rep_near_400");
        if (time.Years == 25) Counters.Set(LeadSign() == 1 ? "mirror_desire_lead_y25" : "mirror_virtue_lead_y25", LeadSign() == 0 ? 0 : 1);
    }

    static int LeadSign()
    {
        int d = MirrorRules.Tenths(CultEvent.TraitAverage(GameSession.Personal, CultEvent.MoodTrait.Desire));
        int v = MirrorRules.Tenths(CultEvent.TraitAverage(GameSession.Personal, CultEvent.MoodTrait.Virtue));
        return d > v ? 1 : d < v ? 2 : 0;
    }

    // 연 1회 입교 (SocialStanding.YearEnd): 실제로 늘거나 줄어든 신도 수
    public static void Recruits(int delta)
    {
        if (Active) Counters.Set("mirror_recruits_year", delta);
    }

    public static void Misfortune(int stacks)
    {
        if (Active) Peak("mirror_misfortune", stacks);
    }

    // 교단 행동 판정 실패 (Turns.Cult)
    public static void Fail()
    {
        if (!Active) return;
        Counters.Add("mirror_fail_acts");
        if (GameSession.CultTime.Turns < GraceTurns) Counters.Add("mirror_early_fails", 1, true);
    }

    // 이벤트가 화면에 뜸 (GameSession.SetPending). 같은 이벤트는 회차를 넘어 한 번만 센다
    public static void Event(string id)
    {
        if (!Active || string.IsNullOrEmpty(id) || id.Length < 3) return;
        var seen = "mirror_ev_" + id;
        if (Counters.Get(seen) > 0) return;
        Counters.Set(seen, 1);
        string group = id.StartsWith("mr") && int.TryParse(id.Substring(2), out var n)
            ? (n <= 50 ? "mirror_mr001" : n <= 100 ? "mirror_mr051" : n <= 150 ? "mirror_mr101" : "mirror_mr151")
            : id.StartsWith("mh") ? "mirror_recovery"
            : id.StartsWith("mf") ? "mirror_final_events" // CSV는 mirror_final 이라 적었지만 엔딩 횟수와 겹쳐 따로 센다
            : "mirror_special";                            // ms / mv / me
        Counters.Add(group);
        Counters.Add("mirror_events");
    }

    // 교리를 배움 (CultStatus.Doctrines.Changed)
    public static void Doctrine(DoctrineSkill skill)
    {
        var tree = GameSession.Cult.Doctrines;
        if (skill.Branch == DoctrineBranch.None || tree.LevelOf(skill) != 1) return; // 처음 배울 때만
        if (!Active)
        {
            if (skill.IsFinal) Counters.Set("normal_final_" + skill.Branch, 1);
            return;
        }
        Counters.Add("mirror_doctrines", 1, true);
        Counters.Add(Key(skill.Branch), 1, true);
        var counts = Branches.Select(b => Counters.Get(Key(b), true)).OrderByDescending(c => c).ToArray();
        Counters.Set("mirror_two_branches", counts[1], true);
        Counters.Set("mirror_four_branches", counts[3], true);
        Counters.Set("mirror_all_branches", counts[7], true);
        if (!skill.IsFinal) return;
        if (Counters.Get("mirror_final_learned", true) > 0) Counters.Set("mirror_switch_final", 1);
        Counters.Add("mirror_final_learned", 1, true);
        if (Counters.Get("normal_final_" + skill.Branch) > 0) Counters.Set("mirror_normal_same_final", 1);
        var own = "mirror_final_" + skill.Id;
        if (Counters.Get(own) == 0) { Counters.Set(own, 1); Counters.Add("mirror_final_all16"); }
    }

    // 엔딩 도달 (GameSession.SetEnding)
    public static void Ending(Endings.Ending ending)
    {
        if (!Active || ending == null || !ending.Id.StartsWith("inv_end_")) return;
        var id = ending.Id;
        Counters.Add("mirror_ending_" + id);
        if (Counters.Get("mirror_ending_" + id) == 1) Counters.Add("mirror_endings_seen");
        if (id is "inv_end_absorbed" or "inv_end_followers") Counters.Add("mirror_collapse");

        if (id.StartsWith(MirrorRules.FinalPrefix))
        {
            Counters.Add("mirror_final");
            var kind = id.EndsWith("_good") ? "good" : "bad";
            var seen = "mirror_seen_final_" + id;
            if (Counters.Get(seen) == 0) { Counters.Set(seen, 1); Counters.Add($"mirror_final_{kind}16"); }
        }

        float d = CultEvent.TraitAverage(GameSession.Personal, CultEvent.MoodTrait.Desire);
        float v = CultEvent.TraitAverage(GameSession.Personal, CultEvent.MoodTrait.Virtue);
        int gap = (int)Math.Round(Math.Abs(d - v), MidpointRounding.AwayFromZero);
        if (d > v) Counters.Max("mirror_gap_desire", gap); else if (v > d) Counters.Max("mirror_gap_virtue", gap);
        Counters.Set("mirror_gap", Math.Min(Counters.Get("mirror_gap"), gap)); // RunStart가 99로 깔아 둔 최솟값
        if (Branches.Count(b => Counters.Get(Key(b), true) > 0) == 1) Counters.Set("mirror_single_branch", 1);
    }
}
