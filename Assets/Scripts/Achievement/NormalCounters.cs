using System;
using System.Linq;
using System.Text.RegularExpressions;

// 본편(생애·교단·성인) 업적용 카운터 훅 모음. 게임 쪽은 한 줄씩 부른다. 신/거울 모드 판은 건드리지 않는다.
// 이번 판 값은 Counters run 범위, 여러 판에 걸친 값은 profile 범위(또는 Profile 카운터)에 쓴다.
// 상태에서 바로 읽히는 값(스탯 균형, 시선 단계, 사망 나이 등)은 Refresh가 턴마다·선택지마다 다시 계산한다.
public static class NormalCounters
{
    static bool Active => !GameSession.GodMode && !GameSession.MirrorMode;

    static int success, fail;                              // 현재 연속 성공/실패 (저장하지 않음: 불러오면 다시 센다)
    static int? prevStanding;
    static int standingChanges, spreadPeak;
    static bool recognized;
    static readonly bool[] lowSeen = new bool[6], highSeen = new bool[6], highFell = new bool[6];

    // GameSession.StartNew에서: 판이 새로 시작될 때 추적 상태를 비운다
    public static void Reset()
    {
        success = fail = standingChanges = spreadPeak = 0;
        prevStanding = null;
        recognized = false;
        Array.Clear(lowSeen, 0, 6); Array.Clear(highSeen, 0, 6); Array.Clear(highFell, 0, 6);
    }

    // Turns.Answer: 선택지 하나를 골랐을 때
    public static void OnAnswer(Choice choice, bool succeeded)
    {
        Profile.Count("events");
        if (!Active) return;
        if (choice.Chance < 1f) // 판정이 있는 선택지만 연속 기록에 센다
        {
            if (succeeded) { success++; fail = 0; Counters.Max("success_streak", success, true); }
            else { fail++; success = 0; Counters.Max("fail_streak", fail, true); }
        }
        // shortcut: 뇌물을 "받는" 선택지는 아직 없어 뇌물/매수 선택지를 고르면 센다. 받는 이벤트가 생기면 그 선택에 맞춰 좁힐 것
        if (Regex.IsMatch(choice.Text, "뇌물|매수|[Bb]ribe|[Bb]uy off|payoff")) Counters.Add("bribes_taken", 1, true);
        Refresh();
    }

    // Turns.Life / Turns.Cult: 행동 하나를 했을 때. 모든 인생·교단 행동을 각 100번 (여러 판 누적, 가장 적게 한 행동 기준)
    public static void OnAction(string key)
    {
        if (!Active) return;
        Counters.Add("n_act_" + key);
        var min = int.MaxValue;
        foreach (LifeAction a in Enum.GetValues(typeof(LifeAction))) min = Math.Min(min, Counters.Get("n_act_life_" + a));
        foreach (CultAction a in Enum.GetValues(typeof(CultAction))) min = Math.Min(min, Counters.Get("n_act_cult_" + CultActions.Key(a)));
        Counters.Set("all_actions", min);
    }

    // GameSession.AdvanceTurn 끝: 상태에서 읽히는 값을 갱신
    public static void Refresh()
    {
        if (!Active) return;
        var personal = GameSession.Personal;
        TrackStanding();

        // 요절: 죽기 전에는 20세 이상 값을 둬 "<=19"가 처음부터 참이 되지 않게 한다
        Counters.Set("death_age", GameSession.Dead ? GameSession.Age.Years : 99, true);

        // 평형(욕망·미덕 8스탯 최고-최저): 교단 10년 이후 관측한 최대 격차. 그 전에는 100 (조건이 일찍 참이 되지 않게)
        var traits = Enumerable.Range(1, 8).Select(i => personal[Endings.StatOf((DoctrineBranch)i)].Value).ToArray();
        if (GameSession.CultFounded && GameSession.CultTime.Years >= Endings.MinYears)
        {
            spreadPeak = Math.Max(spreadPeak, traits.Max() - traits.Min());
            Counters.Set("balanced_desire_virtue", spreadPeak, true);
        }
        else Counters.Set("balanced_desire_virtue", 100, true);

        for (int i = 1; i <= 8; i++) // 한 스탯 100, 반대 스탯 0
        {
            var b = (DoctrineBranch)i;
            if (personal[Endings.StatOf(b)].Value >= PersonalStatus.MaxValue && personal[Endings.StatOf(b.Opposite())].Value <= 0)
                Counters.Set("opposite_extreme", 1, true);
        }

        // 기프티드 계열: 욕망/미덕 계열 5개 이상, 동성애 + 성인 이벤트 5번(이번 판)
        var owned = personal.Talents.Owned;
        if (owned.Count(t => Kind(t) == 2) >= 5) Counters.Set("gifted_special_gifted_beast", 1, true);
        if (owned.Count(t => Kind(t) == 3) >= 5) Counters.Set("gifted_special_gifted_saint", 1, true);
        if (personal.Talents.Has("no.20") && Counters.Get("adult_run_seen", true) >= 5) Counters.Set("gifted_special_no.20", 1, true);

        // 금욕: 성인 이벤트가 켜져 있고(성인 나이) 이번 판에 하나도 안 봤을 때의 교단 햇수
        if (AdultGate.Active && GameSession.CultFounded && Counters.Get("adult_run_seen", true) == 0)
            Counters.Set("adult_none_cultyears", GameSession.CultTime.Years, true);
    }

    // 시선 단계 변화: 오르내림 횟수, 바닥에서 두 단계 회복, 공인→탄압 추락
    static void TrackStanding()
    {
        if (!GameSession.CultFounded) return;
        var cur = (int)SocialStanding.Of(GameSession.Cult);
        if (prevStanding is int p && p != cur) { standingChanges++; Counters.Set("standing_changes", standingChanges, true); }
        prevStanding = cur;
        if (cur >= (int)Standing.Recognized) recognized = true;
        if (recognized && cur <= (int)Standing.Persecuted) Counters.Set("standing_swing", 1, true);
        // 탄압받는 자·감시받는 자: 그 단계까지 내려갔다가 두 단계 이상 올랐다
        for (int x = (int)Standing.Persecuted; x <= (int)Standing.Watched; x++)
        {
            if (cur <= x) lowSeen[x] = true;
            else if (lowSeen[x] && cur >= x + 2) { Counters.Set("standing_recover_" + (Standing)x, 1, true); lowSeen[x] = false; }
        }
        // shortcut: 두 단계 위가 없는 존중·공인은 그 단계에 있다가 두 단계 이상 떨어진 뒤 다시 그 단계로 돌아오면 인정한다
        for (int x = (int)Standing.Respected; x <= (int)Standing.Recognized; x++)
        {
            if (cur >= x) { highSeen[x] = true; if (highFell[x]) { Counters.Set("standing_recover_" + (Standing)x, 1, true); highFell[x] = false; } }
            else if (highSeen[x] && cur <= x - 2) highFell[x] = true;
        }
    }

    // QuestionRunner: 성인 이벤트가 뜰 때 (AdultRecall.Mark 직후)
    public static void OnAdult(string eventId, Question question)
    {
        if (!Active || string.IsNullOrEmpty(eventId) || !AdultTitles.Has(eventId)) return;
        Counters.Set("adult_seen", AdultRecall.SeenCount);
        Counters.Add("adult_run_seen", 1, true);
        Counters.Set("adult_none_cultyears", 0, true);
        // shortcut: 상대 성별을 데이터로 갖고 있지 않아 글의 대명사 수로 가른다. 대명사가 없는 이벤트(부부 등)는 어느 쪽도 세지 않는다
        var text = question.Text + " " + string.Join(" ", question.Choices.Select(c => c.Text));
        var female = Regex.Matches(text, @"그녀|\b(she|her)\b", RegexOptions.IgnoreCase).Count;
        var male = Regex.Matches(text, @"\b(he|him|his)\b", RegexOptions.IgnoreCase).Count + Regex.Matches(text, @"그(?!녀)(?=[는가를의에게도]|\s)").Count;
        if (female > male) Counters.Set("adult_f", 1, true);
        else if (male > female) Counters.Set("adult_m", 1, true);
        if (Counters.Get("adult_f", true) > 0 && Counters.Get("adult_m", true) > 0) Counters.Set("adult_both_genders", 1, true);
        Refresh();
    }

    // CultFlow.OnDoctrineChanged: 교리를 익혔을 때 (최종 스킬 / 한 갈래 완성)
    public static void OnDoctrine(DoctrineSkill skill)
    {
        if (!Active || skill == null || skill.Branch == DoctrineBranch.None) return;
        if (skill.IsFinal) Counters.Set("final_branch_" + skill.Branch, 1);
        var tree = GameSession.Cult.Doctrines;
        var b = skill.Branch;
        // shortcut: 6단계부터 A/B가 배타라 "전부"는 불가능하다. 1~5단계 전부 최대 + A 또는 B 한 갈래의 6~10단계 전부로 본다
        bool Full(string path) => Enumerable.Range(6, 5).All(t => DoctrineCatalog.At(b, t, path) is { } s && tree.LevelOf(s) > 0);
        if (Enumerable.Range(1, 5).All(t => DoctrineCatalog.At(b, t) is { } s && tree.IsMax(s)) && (Full("A") || Full("B")))
            Counters.Set("branch_complete", 1);
    }

    // GameSession.FoundCult: 교단 창설 시점
    public static void OnFounded()
    {
        if (!Active) return;
        Counters.Set("found_age", GameSession.Age.Years, true);
        var owned = GameSession.Personal.Talents.Owned;
        if (owned.Count == 0) Counters.Set("gifted_special_gifted_none", 1, true);
        else if (owned.All(t => Kind(t) == 1)) Counters.Set("gifted_special_gifted_cleric", 1, true);
    }

    // TalentList.Add: 기프티드 도감 (한 번이라도 가져 본 종류 수, 여러 판 누적)
    public static void OnGifted(Talent talent)
    {
        if (Profile.CountOf("gseen_" + talent.Id) > 0) return;
        Profile.Count("gseen_" + talent.Id);
        Profile.Count("gifted_seen");
    }

    // Progress.Reach: 엔딩에 도달했을 때 (Endings.Reach 이후, Achievements.Evaluate 이전)
    public static void OnEnding(Endings.Ending ending)
    {
        Profile.Count("endings_seen", Endings.SeenCount - Profile.CountOf("endings_seen"));
        if (!Active) return;
        Refresh();
        for (int i = 1; i <= 8; i++)
        {
            var b = (DoctrineBranch)i;
            bool Seen(EndingKind k) => Endings.All.Any(e => e.Final != null && e.Final.Branch == b && e.Kind == k && Endings.Seen(e.Id));
            if (Seen(EndingKind.Good) && Seen(EndingKind.Bad)) Counters.Set("both_endings_" + b, 1);
        }
        if (Endings.All.Where(e => e.Kind == EndingKind.Good).All(e => Endings.Seen(e.Id))) Counters.Set("all_good_endings", 1);
        if (Endings.All.Where(e => e.Kind == EndingKind.Bad).All(e => Endings.Seen(e.Id))) Counters.Set("all_bad_endings", 1);
    }

    // 기프티드 계열 (스탯 보정 합 기준 근사): 1 신앙, 2 욕망, 3 미덕, 0 해당 없음
    // shortcut: 기프티드에 "계열" 필드가 없어 양(+)의 스탯 보정이 가장 큰 쪽으로 가른다. 분류 필드가 생기면 교체
    static int Kind(Talent t)
    {
        float faith = 0, desire = 0, virtue = 0;
        foreach (var m in t.Modifiers)
        {
            if (m.Percent <= 0) continue;
            if (m.PersonalStat is { } p)
            {
                if (p is PersonalStatId.Piety or PersonalStatId.Miracle) faith += m.Percent;
                else if (p is PersonalStatId.Lust or PersonalStatId.Greed or PersonalStatId.Appetite or PersonalStatId.Ambition) desire += m.Percent;
                else if (p is PersonalStatId.Temperance or PersonalStatId.Charity or PersonalStatId.Patience or PersonalStatId.Integrity) virtue += m.Percent;
            }
            else if (m.CultStat is StatId.LeaderFaith or StatId.FollowerFaith or StatId.Communion) faith += m.Percent;
        }
        var max = Math.Max(faith, Math.Max(desire, virtue));
        return max <= 0 ? 0 : faith == max ? 1 : desire == max ? 2 : 3;
    }
}
