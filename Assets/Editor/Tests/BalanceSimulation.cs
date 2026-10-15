using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

// 밸런스 시뮬레이션: 목표 엔딩(최종 스킬 가지 + 굿/배드)을 정해 둔 봇이 업적·엔딩 기록을 이어 가며 여러 회차를 돈다.
// 목표 엔딩을 처음 본 회차의 분포로 난이도를 잰다. 기록은 판마다 새로 만든 임시 프로필에만 남는다.
public static class BalanceSimulation
{
    public const int MaxCultYears = 60;   // 이만큼 지나도 엔딩이 없으면 그 판을 접고 새로 시작한 것으로 본다

    public class Report
    {
        public int Careers, MaxRuns;
        public readonly List<int> FirstTarget = new();   // 목표 엔딩을 처음 본 회차 (못 보면 MaxRuns + 1)
        public readonly List<int> FirstFinal = new();    // 아무 최종 스킬 엔딩이든 처음 본 회차
        public readonly Dictionary<string, int> Endings = new();
        public readonly SortedDictionary<string, List<int>> ByGoal = new(); // 목표 종류별 첫 달성 회차
        public int Runs, CultYearsTotal, Deaths, BeforeFounding;

        public string Format(string title)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{title}] 경력 {Careers}명 × 최대 {MaxRuns}회차, 판 {Runs}개");
            sb.AppendLine($"  목표 엔딩 첫 달성 회차: 평균 {Mean(FirstTarget):0.00} / 중앙값 {Median(FirstTarget)} (미달성 {FirstTarget.Count(r => r > MaxRuns)}명)");
            sb.AppendLine($"  최종 엔딩(아무거나) 첫 달성 회차: 평균 {Mean(FirstFinal):0.00} / 중앙값 {Median(FirstFinal)}");
            sb.Append("  회차별 누적 목표 달성률:");
            for (int run = 1; run <= MaxRuns; run++) sb.Append($" {run}:{100f * FirstTarget.Count(r => r <= run) / Careers:0}%");
            sb.AppendLine();
            foreach (var pair in ByGoal)
            {
                sb.Append($"  {pair.Key} ({pair.Value.Count}명): 평균 {Mean(pair.Value):0.00}회차, 누적");
                foreach (var run in new[] { 1, 3, 5, 8, MaxRuns }) sb.Append($" {run}:{100f * pair.Value.Count(r => r <= run) / pair.Value.Count:0}%");
                sb.AppendLine();
            }
            sb.AppendLine($"  판당 평균 교단 운영 {(Runs == 0 ? 0 : (float)CultYearsTotal / Runs):0.0}년, 사망 {Deaths}판 (창설 전 {BeforeFounding})");
            sb.AppendLine("  엔딩 분포: " + string.Join(", ", Endings.OrderByDescending(p => p.Value).Select(p => $"{p.Key} {p.Value}")));
            return sb.ToString();
        }

        static float Mean(List<int> xs) => xs.Count == 0 ? 0 : (float)xs.Average();
        static int Median(List<int> xs) { if (xs.Count == 0) return 0; var s = xs.OrderBy(x => x).ToList(); return s[s.Count / 2]; }
    }

    static readonly DoctrineBranch[] Branches =
    {
        DoctrineBranch.Lust, DoctrineBranch.Greed, DoctrineBranch.Appetite, DoctrineBranch.Ambition,
        DoctrineBranch.Temperance, DoctrineBranch.Charity, DoctrineBranch.Patience, DoctrineBranch.Integrity,
    };
    static readonly PersonalStatId[] Desire = { PersonalStatId.Lust, PersonalStatId.Greed, PersonalStatId.Appetite, PersonalStatId.Ambition };
    static readonly PersonalStatId[] Virtue = { PersonalStatId.Temperance, PersonalStatId.Charity, PersonalStatId.Patience, PersonalStatId.Integrity };
    static readonly PersonalStatId[] HealthGroup = { PersonalStatId.Health, PersonalStatId.Mentality, PersonalStatId.Piety, PersonalStatId.Repute, PersonalStatId.Wealth };

    public static Report Run(int careers, int maxRuns, int seed)
    {
        var previousPath = Profile.Path;
        var previousEnabled = Profile.Enabled;
        var report = new Report { Careers = careers, MaxRuns = maxRuns };
        try
        {
            for (int c = 0; c < careers; c++)
            {
                var temp = Path.Combine(Path.GetTempPath(), $"xxxcult_balance_{Guid.NewGuid():N}.json");
                Profile.Path = temp;
                Profile.Enabled = true;
                var rng = new Random(seed + c * 7919);
                var goal = new Goal(Branches[rng.Next(Branches.Length)], rng.Next(2) == 0 ? EndingKind.Good : EndingKind.Bad);
                int firstTarget = maxRuns + 1, firstFinal = maxRuns + 1;
                for (int run = 1; run <= maxRuns; run++)
                {
                    var ending = PlayRun(run, goal, rng.Next(), report);
                    report.Runs++;
                    var key = ending?.Id ?? "none";
                    report.Endings[key] = report.Endings.TryGetValue(key, out var n) ? n + 1 : 1;
                    if (ending?.Final != null && firstFinal > maxRuns) firstFinal = run;
                    if (ending?.Final != null && ending.Final.Branch == goal.Branch && ending.Kind == goal.Kind) { firstTarget = run; break; }
                }
                report.FirstTarget.Add(firstTarget);
                var category = $"{(Endings.IsDesireBranch(goal.Branch) ? "욕망" : "덕목")} 가지 {(goal.Kind == EndingKind.Good ? "굿" : "배드")}";
                if (!report.ByGoal.TryGetValue(category, out var list)) report.ByGoal[category] = list = new List<int>();
                list.Add(firstTarget);
                report.FirstFinal.Add(firstFinal);
                Profile.Reset();
                if (File.Exists(temp)) File.Delete(temp);
            }
        }
        finally
        {
            Profile.Enabled = previousEnabled;
            Profile.Path = previousPath;
            GameSession.StartNew(1);
        }
        return report;
    }

    // 목표: 이 가지의 최종 스킬로 이 굿/배드 엔딩을 본다
    sealed class Goal
    {
        public readonly DoctrineBranch Branch;
        public readonly EndingKind Kind;
        public readonly PersonalStatId BranchStat;
        public readonly PersonalStatId[] Opposite;
        public readonly bool WantOppositeHigh; // 반대쪽 평균을 50 위로 올려야 하는가

        public Goal(DoctrineBranch branch, EndingKind kind)
        {
            Branch = branch; Kind = kind;
            BranchStat = (PersonalStatId)Enum.Parse(typeof(PersonalStatId), branch.ToString());
            var desire = Endings.IsDesireBranch(branch);
            Opposite = desire ? Virtue : Desire;
            // 욕망 가지: 미덕 평균 > 50 이면 굿 / 미덕 가지: 욕망 평균 > 50 이면 배드
            WantOppositeHigh = desire ? kind == EndingKind.Good : kind == EndingKind.Bad;
        }
    }

    static Endings.Ending PlayRun(int run, Goal goal, int seed, Report report)
    {
        GameSession.StartNew(seed);
        Progress.BeginRun(Progress.NextRun); // 실제 게임처럼 지금까지 본 엔딩 수로 회차가 정해진다
        Achievements.ApplyStartRewards(GameSession.Personal);
        GameSession.Personal.Gender = seed % 2 == 0 ? Gender.Male : Gender.Female;
        GameSession.MarkStarted();

        // 생애: 이벤트(답하면 1턴) → 행동(1턴)을 번갈아 18세 4분기까지
        while (GameSession.Age.Turns < LifeFlow.FoundingTurn)
        {
            Turns.Answer(Best(Turns.NextLifeEvent().Question, goal));
            GameSession.AdvanceTurn();
            if (Progress.ForcedEnding() is { } dead) { report.Deaths++; report.BeforeFounding++; return Finish(dead); }
            if (GameSession.Age.Turns >= LifeFlow.FoundingTurn) break;
            Turns.Life(BestLifeAction(goal));
            if (Progress.ForcedEnding() is { } dead2) { report.Deaths++; report.BeforeFounding++; return Finish(dead2); }
        }

        GameSession.FoundCult("시뮬교단");
        GameSession.Cult.Doctrines.LevelUp(DoctrineSkill.Declaration);
        string path = null;

        while (GameSession.CultTime.Years < MaxCultYears)
        {
            Rest();
            if (Progress.ForcedEnding() is { } forced) return Finish(forced, report);
            LearnDoctrine(goal, ref path);
            if (Progress.DueEnding() is { } due) return Finish(due, report);

            Turns.Cult(BestCultAction(goal));
            if (Progress.ForcedEnding() is { } afterAction) return Finish(afterAction, report);
            var next = Turns.NextCultEvent();
            if (next != null) Turns.Answer(Best(next.Question, goal));
            if (Progress.ForcedEnding() is { } afterEvent) return Finish(afterEvent, report);
        }
        report.CultYearsTotal += GameSession.CultTime.Years;
        return null;
    }

    static Endings.Ending Finish(Endings.Ending ending, Report report = null)
    {
        if (report != null)
        {
            report.CultYearsTotal += GameSession.CultTime.Years;
            if (Progress.IsDeath(ending)) report.Deaths++;
        }
        Progress.Reach(ending);
        return ending;
    }

    static void Rest()
    {
        for (int guard = 0; guard < 50; guard++)
        {
            var resting = global::Rest.ZeroStats(GameSession.Personal);
            if (resting.Count == 0 || Progress.ForcedEnding() != null) return;
            for (int turn = 0; turn < global::Rest.Turns; turn++)
            {
                global::Rest.ApplyTurn(GameSession.Personal, GameSession.Cult, resting);
                GameSession.AdvanceTurn();
            }
        }
    }

    // 목표 가지를 1단계부터 차례로 배운다 (6단계 이후 갈래는 배울 수 있는 쪽)
    static void LearnDoctrine(Goal goal, ref string path)
    {
        var tree = GameSession.Cult.Doctrines;
        for (int tier = 1; tier <= 10; tier++)
        {
            DoctrineSkill skill;
            if (tier < 6) skill = DoctrineCatalog.At(goal.Branch, tier, "");
            else
            {
                path ??= "A";
                skill = DoctrineCatalog.At(goal.Branch, tier, path);
            }
            if (skill == null || tree.LevelOf(skill) > 0) continue;
            if (skill.IsFinal && !OppositeReady(goal)) return; // 굿/배드 쪽이 맞을 때까지 최종 스킬을 미룬다
            var points = DoctrinePoints.Available(GameSession.Cult, GameSession.CultTime, Profile.UnlockedCount);
            if (!tree.CanLearn(skill, points)) return;
            tree.LevelUp(skill);
        }
    }

    static float OppositeAverage(Goal goal) =>
        CultEvent.TraitAverage(GameSession.Personal, Endings.IsDesireBranch(goal.Branch) ? CultEvent.MoodTrait.Virtue : CultEvent.MoodTrait.Desire);

    static bool OppositeReady(Goal goal) => goal.WantOppositeHigh ? OppositeAverage(goal) > 55 : OppositeAverage(goal) <= 45;

    // ---- 선택 점수: 목표에 맞는 개인 스탯은 올리고, 교단은 해체되지 않게 지킨다

    static float PersonalWeight(PersonalStatId id, Goal goal)
    {
        var p = GameSession.Personal;
        if (id == goal.BranchStat) return 3f;
        if (Array.IndexOf(goal.Opposite, id) >= 0)
        {
            var avg = OppositeAverage(goal);
            if (goal.WantOppositeHigh) return avg > 60 ? 0.2f : 1.2f;
            return avg < 40 ? 0f : -1.2f;
        }
        if (Array.IndexOf(HealthGroup, id) >= 0) return p[id].Value < 10 ? 1.5f : 0.4f;
        return 0.2f;
    }

    static float CultWeight(StatId id)
    {
        var cult = GameSession.Cult;
        switch (id)
        {
            case StatId.Reputation:
                var rep = cult[StatId.Reputation].Value;
                return rep < 350 ? 0.4f : rep < 500 ? 0.12f : 0.03f;
            case StatId.Doctrine: return 0.05f;
            case StatId.Offerings: return cult[StatId.Offerings].Value < 100 ? 0.05f : 0.01f;
            case StatId.Followers: return 0.05f;
            case StatId.Communion: return 0f;
            default: return 0.02f;
        }
    }

    static float Score(IEnumerable<StatEffect> personal, IEnumerable<CultEffect> cult, Goal goal)
    {
        float s = 0;
        foreach (var e in personal) s += PersonalWeight(e.Stat, goal) * e.Amount;
        foreach (var e in cult) s += CultWeight(e.Stat) * e.Amount;
        return s;
    }

    static float Score(Outcome outcome, Goal goal) => outcome == null ? 0 : Score(outcome.Effects, outcome.CultEffects, goal);

    static Choice Best(Question question, Goal goal)
    {
        Choice best = null;
        float bestScore = float.MinValue;
        foreach (var choice in question.Choices)
        {
            var p = choice.ChanceFor(GameSession.Personal);
            var score = p * Score(choice.Success, goal) + (1 - p) * Score(choice.Fail, goal);
            if (score > bestScore) { bestScore = score; best = choice; }
        }
        return best;
    }

    static LifeAction BestLifeAction(Goal goal)
    {
        var personal = GameSession.Personal;
        LifeAction best = LifeAction.Exercise;
        float bestScore = float.MinValue;
        foreach (LifeAction action in Enum.GetValues(typeof(LifeAction)))
        {
            var p = ActionCheck.Chance(personal[LifeActions.KeyStat(action)].Value / (float)PersonalStatus.MaxValue,
                GiftedSpecials.ChanceBonus(personal.Talents, action.ToString()));
            var score = p * Score(LifeActions.EffectsOf(action, true), Array.Empty<CultEffect>(), goal)
                        + (1 - p) * Score(LifeActions.EffectsOf(action, false), Array.Empty<CultEffect>(), goal);
            if (score > bestScore) { bestScore = score; best = action; }
        }
        return best;
    }

    static CultAction BestCultAction(Goal goal)
    {
        var cult = GameSession.Cult;
        CultAction best = CultAction.Preach;
        float bestScore = float.MinValue;
        foreach (CultAction action in Enum.GetValues(typeof(CultAction)))
        {
            var p = ActionCheck.Chance(cult.Ratio(CultActions.KeyStat(action)), GiftedSpecials.ChanceBonus(GameSession.Personal.Talents, CultActions.Key(action)));
            var score = p * Score(CultActions.PersonalEffectsOf(action, true), CultActions.CultEffectsOf(action, true), goal)
                        + (1 - p) * Score(CultActions.PersonalEffectsOf(action, false), CultActions.CultEffectsOf(action, false), goal);
            if (score > bestScore) { bestScore = score; best = action; }
        }
        return best;
    }
}

public class BalanceSimulationTests
{
    // 일반 테스트 실행에는 끼지 않는다. 직접 실행하면 결과를 임시 폴더의 xxxcult_balance_report.txt에 남긴다.
    [Test, Explicit]
    public void Simulate()
    {
        var careers = int.TryParse(System.Environment.GetEnvironmentVariable("XXXCULT_SIM_CAREERS"), out var n) ? n : 200;
        var report = BalanceSimulation.Run(careers, 12, 20261015);
        var text = report.Format("현재 규칙");
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "xxxcult_balance_report.txt"), text);
        TestContext.WriteLine(text);
    }

    [Test]
    public void SmallSimulation_RunsWithoutErrors()
    {
        var report = BalanceSimulation.Run(2, 2, 7);
        Assert.Greater(report.Runs, 0);
    }
}
