using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

// 준비된 신 모드 밸런스 시뮬레이션: 목표 승천(먼저 완성할 두 핵)을 정한 봇이 여러 판을 돈다.
// 이 모드엔 회차 난이도가 없어서 판마다 규칙이 같다. 기록은 판마다 새로 만든 임시 프로필에만 남는다.
public static class GodSimulation
{
    // Rush: 목표 두 가지를 바깥부터 핵까지 차례로 올린다 / Spread: 고리마다 모든 가지를 넓게 찍어 스킬 총량으로 버틴다
    // Balanced: 목표 두 가지는 핵까지, 나머지 두 가지는 바깥 두 고리까지만 넓혀 둔다 (핵 루트와 분산의 중간)
    public enum Strategy { Rush, Spread, Balanced }

    public const int MaxCultYears = 60; // 이만큼 지나도 엔딩이 없으면 그 판을 접은 것으로 본다

    public class Report
    {
        public int Bots, RunsPerBot;
        public Strategy Strategy;
        public readonly List<RunResult> Results = new();
        public readonly List<int> FirstAscension = new(); // 봇마다 처음 승천한 판 (못 하면 RunsPerBot + 1)

        public string Format()
        {
            var sb = new StringBuilder();
            var runs = Results.Count;
            var asc = Results.Where(r => r.Ascended).ToList();
            sb.AppendLine($"[준비된 신 · {Strategy switch { Strategy.Rush => "핵 루트 봇", Strategy.Spread => "분산 봇", _ => "절충 봇" }}] 봇 {Bots}명 × {RunsPerBot}판 = {runs}판");
            sb.AppendLine($"  승천률 {Pct(asc.Count, runs)} (목표한 조합 그대로 {Pct(Results.Count(r => r.HitGoal), runs)}), 패배율 {Pct(Results.Count(r => r.Defeated), runs)}");
            foreach (var group in new[] { false, true })
            {
                var g = Results.Where(r => r.HiddenGoal == group).ToList();
                var ga = g.Where(r => r.Ascended).ToList();
                sb.AppendLine($"  {(group ? "히든 목표(같은 계열 핵 둘)" : "일반 목표(신격+인격)")} {g.Count}판: 목표 달성 {Pct(g.Count(r => r.HitGoal), g.Count)}, 승천 {Pct(ga.Count, g.Count)}, 패배 {Pct(g.Count(r => r.Defeated), g.Count)}, 승천 평균 {Avg(ga, r => r.Years):0.0}년");
            }
            sb.AppendLine($"  이벤트 성공률 감점 평균 {100 * Avg(Results, r => r.Penalty):0.0}%p (최고치 평균)");
            sb.AppendLine($"  최고 압박 단계 평균: 신계 {Avg(Results, r => r.MaxHeaven):0.00} / 지상 {Avg(Results, r => r.MaxEarth):0.00}");
            sb.AppendLine($"  승천까지 교단 연수: 평균 {Avg(asc, r => r.Years):0.0}년 / 중앙값 {Median(asc.Select(r => r.Years))}년 / 최소 {(asc.Count == 0 ? 0 : asc.Min(r => r.Years))} 최대 {(asc.Count == 0 ? 0 : asc.Max(r => r.Years))}");
            sb.AppendLine($"  판당 평균 교단 연수 {Avg(Results, r => r.Years):0.0}년, 권능 포인트 획득 {Avg(Results, r => r.PointsEarned):0.0} (연평균 {Results.Sum(r => r.PointsEarned) / (float)Math.Max(1, Results.Sum(r => r.Years)):0.00}), 익힌 노드 {Avg(Results, r => r.Learned):0.0}개, 도달 단계 {Avg(Results, r => r.Stage):0.00}");
            sb.AppendLine($"  봇별 첫 승천 판: 평균 {(FirstAscension.Count == 0 ? 0 : FirstAscension.Average()):0.00} (10판 안에 한 번도 못 한 봇 {FirstAscension.Count(r => r > RunsPerBot)}명)");
            sb.AppendLine($"  끝날 때 평균: 신도 {Avg(Results, r => r.Followers):0}명, 신도 믿음 {Avg(Results, r => r.FollowerFaith):0}, 명망 {Avg(Results, r => r.Reputation):0}, 체력 {Avg(Results, r => r.Health):0.0}, 정신 {Avg(Results, r => r.Mentality):0.0}");
            sb.AppendLine("  엔딩 분포: " + string.Join(", ", Results.GroupBy(r => r.EndingId).OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key} {g.Count()} ({Pct(g.Count(), runs)}, 평균 {g.Average(r => r.Years):0.0}년)")));
            sb.Append("  판 번호별 승천률:");
            for (int run = 1; run <= RunsPerBot; run++)
            {
                var atRun = Results.Where(r => r.Run == run).ToList();
                sb.Append($" {run}:{Pct(atRun.Count(r => r.Ascended), atRun.Count)}");
            }
            sb.AppendLine();
            return sb.ToString();
        }

        static string Pct(int n, int total) => total == 0 ? "0%" : $"{100f * n / total:0.0}%";
        static float Avg(List<RunResult> xs, Func<RunResult, float> f) => xs.Count == 0 ? 0 : xs.Average(f);
        static int Median(IEnumerable<int> xs) { var s = xs.OrderBy(x => x).ToList(); return s.Count == 0 ? 0 : s[s.Count / 2]; }
    }

    public class RunResult
    {
        public int Run, Years, PointsEarned, Learned, Stage, Followers, FollowerFaith, Reputation, Health, Mentality;
        public string EndingId;
        public bool Ascended, HitGoal, HiddenGoal, Defeated;
        public int MaxHeaven, MaxEarth;
        public float Penalty;
    }

    static readonly GodBranch[] Branches = { GodBranch.Miracle, GodBranch.Prophecy, GodBranch.Mercy, GodBranch.Desire };

    // pool: 봇이 고를 가지 목록 (null이면 전체). 욕망형/미덕형 분할에 쓴다.
    public static Report Run(int bots, int runsPerBot, int seed, Strategy strategy = Strategy.Rush, GodBranch[] pool = null)
    {
        var previousPath = Profile.Path;
        var previousEnabled = Profile.Enabled;
        var report = new Report { Bots = bots, RunsPerBot = runsPerBot, Strategy = strategy };
        try
        {
            for (int b = 0; b < bots; b++)
            {
                var temp = Path.Combine(Path.GetTempPath(), $"xxxcult_god_{Guid.NewGuid():N}.json");
                Profile.Path = temp;
                Profile.Enabled = true;
                var rng = new Random(seed + b * 7919);
                var choices = pool ?? Branches;
                var first = choices[rng.Next(choices.Length)];
                var second = choices.Where(x => x != first).ElementAt(rng.Next(choices.Length - 1));
                int firstAscension = runsPerBot + 1;
                for (int run = 1; run <= runsPerBot; run++)
                {
                    var result = PlayRun(run, first, second, rng.Next(), strategy);
                    report.Results.Add(result);
                    if (result.Ascended && firstAscension > runsPerBot) firstAscension = run;
                }
                report.FirstAscension.Add(firstAscension);
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

    internal static RunResult PlayRun(int run, GodBranch first, GodBranch second, int seed, Strategy strategy)
    {
        GodRules.Begin(seed);
        if (Trace != null && run == 1) tracePlayer++;
        GameSession.Personal.Gender = seed % 2 == 0 ? Gender.Male : Gender.Female;
        GameSession.MarkStarted();
        GameSession.FoundCult("시뮬신전");

        var tree = GameSession.God.Tree;
        // 분산 봇은 고리 순서로 네 가지를 번갈아 찍는다. 핵 두 개가 먼저 완성되는 쪽이 승천 조합이 된다.
        var plan = (strategy switch
            {
                Strategy.Rush => Plan(first).Concat(Plan(second)),
                Strategy.Spread => GodSkillCatalog.All.Where(s => !s.IsCenter).OrderBy(s => s.Ring).ThenBy(s => s.Branch == first ? 0 : s.Branch == second ? 1 : 2),
                _ => GodSkillCatalog.All.Where(s => !s.IsCenter && (s.Branch == first || s.Branch == second || s.Ring <= 2))
                    .OrderBy(s => s.Ring).ThenBy(s => s.Branch == first ? 0 : s.Branch == second ? 1 : 2),
            })
            .Append(GodSkillCatalog.Ascension).ToList();
        tree.ChooseNature(plan.First(s => s.IsOuter)); // 첫 목표 가지의 바깥 노드 하나를 본성으로
        // 다섯 건물을 모두 짓도록 해금 노드와 그 선행(바깥쪽 자식 전부)을 먼저 찍는다
        plan = GodBuildings.UnlockNodes.Keys.Select(GodSkillCatalog.Find).SelectMany(Subtree).OrderBy(s => s.Ring)
            .Concat(plan).Distinct().ToList();

        Endings.Ending ending = null;
        int maxHeaven = 0, maxEarth = 0;
        float penalty = 0;
        while (ending == null && GameSession.CultTime.Years < MaxCultYears)
        {
            maxHeaven = Math.Max(maxHeaven, GameSession.God.Pressure.Stage(Pressure.Heaven, GameSession.Cult));
            maxEarth = Math.Max(maxEarth, GameSession.God.Pressure.Stage(Pressure.Earth, GameSession.Cult));
            penalty = Math.Max(penalty, GodRules.EventPenalty);
            BalanceSimulation.Rest();
            if ((ending = Progress.ForcedEnding()) != null) break;
            Learn(plan);
            if ((ending = Progress.DueEnding()) != null) break;
            var why = Trace != null ? WhyNotBuilt() : null;
            builtType = null;
            string act = null;
            if (!Consecrate()) { var a = ChooseAction(); Turns.Cult(a); act = a.ToString(); }
            if ((ending = Progress.ForcedEnding()) != null) { TraceTurn(run, act, why); break; }
            var next = Turns.NextCultEvent();
            if (next != null) Turns.Answer(Best(next.Question));
            ending = Progress.ForcedEnding();
            TraceTurn(run, act, why);
        }
        if (ending != null) Progress.Reach(ending);

        var personal = GameSession.Personal;
        var cult = GameSession.Cult;
        return new RunResult
        {
            Run = run,
            Years = GameSession.CultTime.Years,
            EndingId = ending?.Id ?? "none",
            Ascended = ending != null && GodRules.IsAscension(ending),
            HitGoal = ending != null && ending.Id == GodEndings.IdFor(first, second),
            HiddenGoal = GodEndings.IdFor(first, second) is GodEndings.DivinePair or GodEndings.HumanPair,
            Defeated = ending != null && !GodRules.IsAscension(ending),
            MaxHeaven = maxHeaven,
            MaxEarth = maxEarth,
            Penalty = penalty,
            PointsEarned = GameSession.God.Points.Earned,
            Learned = tree.Learned.Count(),
            Stage = tree.ReachedStage,
            Followers = cult[StatId.Followers].Value,
            FollowerFaith = cult[StatId.FollowerFaith].Value,
            Reputation = cult[StatId.Reputation].Value,
            Health = personal[PersonalStatId.Health].Value,
            Mentality = personal[PersonalStatId.Mentality].Value,
        };
    }

    // 한 가지(핵 포함 32개)를 바깥부터 안쪽 순서로
    static IEnumerable<GodSkill> Plan(GodBranch branch)
    {
        var core = GodSkillCatalog.Cores.First(c => c.Branch == branch);
        return Subtree(core).OrderBy(s => s.Ring);
    }

    static IEnumerable<GodSkill> Subtree(GodSkill skill) => new[] { skill }.Concat(skill.Children.SelectMany(Subtree));

    // 계획 순서대로, 지금 익힐 수 있는 것부터 포인트가 닿는 만큼 익힌다
    static void Learn(List<GodSkill> plan)
    {
        var state = GameSession.God;
        bool learned;
        do
        {
            learned = false;
            foreach (var skill in plan)
            {
                if (state.Tree.IsLearned(skill) || !state.Tree.CanLearn(skill, state.Available)) continue;
                state.Tree.Learn(skill, state.Available);
                learned = true;
                break;
            }
        } while (learned);
    }

    // 그릇이 위태로우면 향연, 접신이 모자라면 교감, 명망·믿음이 낮으면 지키고, 아니면 이적으로 신도를 늘린다
    // 지을 수 있는 건물이 있고 봉헌금이 유지비까지 넉넉하면 축성한다. 구호소(명망 하한)가 해산을 막는 첫 방벽이다.
    static readonly GodBuildingType[] BuildOrder =
        { GodBuildingType.Shelter, GodBuildingType.Temple, GodBuildingType.Village, GodBuildingType.Library, GodBuildingType.Statue };

    static bool Consecrate()
    {
        var god = GameSession.God;
        var cult = GameSession.Cult;
        // 가장 낮은 건물부터 (앞 순서만 짓지 않게). 판당 한두 채라 같은 레벨이면 쉼터 다음 누적 건설이 적은 것부터 돌아가며 짓는다
        foreach (var type in BuildOrder.OrderBy(god.Buildings.LevelOf)
                     .ThenBy(t => t == GodBuildingType.Shelter ? -1 : Counters.Get($"god_build_{t}")))
        {
            if (god.Buildings.Blocker(type, god.Tree, cult) != null) continue;
            var next = god.Buildings.LevelOf(type) + 1;
            if (cult[StatId.Offerings].Value < GodBuildings.CostOf(next) + GodBuildings.UpkeepOf(next) * 2) continue;
            builtType = type;
            return Turns.Consecrate(type);
        }
        return false;
    }

    // 진단 기록 (BuildProbe 전용, null이면 끔): 턴마다 행동·봉헌금·압박·건물 레벨
    internal static StringBuilder Trace;
    static string traceLabel;
    static int tracePlayer;
    static GodBuildingType? builtType;

    // 봇이 이번 턴에 못 짓는 이유: 해금된 건물 중 가장 가까운 것의 막힘 (guard = 유지비 2년치 여유 부족)
    static string WhyNotBuilt()
    {
        var god = GameSession.God;
        var cult = GameSession.Cult;
        string best = "none";
        foreach (var type in BuildOrder.OrderBy(god.Buildings.LevelOf))
        {
            var blocker = god.Buildings.Blocker(type, god.Tree, cult);
            if (blocker == "locked" || blocker == "max") continue;
            if (blocker == null)
            {
                var next = god.Buildings.LevelOf(type) + 1;
                return cult[StatId.Offerings].Value < GodBuildings.CostOf(next) + GodBuildings.UpkeepOf(next) * 2 ? "guard" : "ok";
            }
            if (best == "none") best = blocker;
        }
        return best;
    }

    static void TraceTurn(int run, string act, string why)
    {
        if (Trace == null) return;
        var god = GameSession.God;
        var cult = GameSession.Cult;
        Trace.Append(traceLabel).Append('\t').Append(tracePlayer).Append('\t').Append(run).Append('\t')
            .Append(GameSession.CultTime.Turns).Append('\t').Append(GameSession.CultTime.Years).Append('\t')
            .Append(builtType != null ? $"C:{builtType}" : act).Append('\t').Append(why).Append('\t')
            .Append(cult[StatId.Offerings].Value).Append('\t').Append(cult[StatId.Authority].Value).Append('\t')
            .Append(cult[StatId.Reputation].Value).Append('\t').Append(cult[StatId.Followers].Value).Append('\t')
            .Append(god.Pressure.Stage(Pressure.Earth, cult)).Append('\t').Append(god.Pressure.Stage(Pressure.Heaven, cult)).Append('\t')
            .Append(god.Tree.ReachedStage).Append('\t')
            .Append(string.Join("", GodBuildings.All.Select(t => GodBuildings.IsUnlocked(t, god.Tree) ? "1" : "0"))).Append('\t')
            .Append(string.Join("/", god.Buildings.Levels)).Append('\t').Append(Counters.Get("god_seized", true)).Append('\n');
    }

    // 스타일당 players명 × runs판을 돌려 Temp/build_probe.tsv에 쓴다. 끝나면 Temp/build_probe_done.txt
    public static void BuildProbe(int players = 3, int runs = 5)
    {
        var path = Path.Combine("Temp", "build_probe.tsv");
        var done = Path.Combine("Temp", "build_probe_done.txt");
        if (File.Exists(done)) File.Delete(done);
        Trace = new StringBuilder("style\tplayer\trun\tturn\tyear\tact\twhy\toff\tauth\trep\tfollowers\tearth\theaven\tstage\tunlock(StTeLiViSh)\tlevels\tseized\n");
        try
        {
            foreach (AchievementSimulation.Style style in Enum.GetValues(typeof(AchievementSimulation.Style)))
            {
                traceLabel = style.ToString();
                tracePlayer = 0;
                AchievementSimulation.Collect(AchievementSimulation.SimMode.God, style, players, runs, 20261020 + (int)style * 1000 + 200000);
            }
            File.WriteAllText(path, Trace.ToString(), new UTF8Encoding(false));
            File.WriteAllText(done, "ok");
        }
        catch (Exception e) { File.WriteAllText(done, "fail " + e); }
        finally { Trace = null; }
    }

    static CultAction ChooseAction()
    {
        var personal = GameSession.Personal;
        var cult = GameSession.Cult;
        if (personal[PersonalStatId.Health].Value <= 25 || personal[PersonalStatId.Mentality].Value <= 25) return CultAction.Desire;
        if (cult[StatId.Communion].Value < GodRules.CommunionCost(CultAction.Proselytize)) return CultAction.Pray;
        if (cult[StatId.Reputation].Value < 300) return CultAction.Serve;       // 지상 압박 대응
        if (cult[StatId.FollowerFaith].Value < 300) return CultAction.Preach;   // 신계 압박 대응
        if (cult[StatId.Followers].Value < 30) return CultAction.Proselytize;
        return CultAction.Proselytize;
    }

    static float Weight(PersonalStatId id)
    {
        var value = GameSession.Personal[id].Value;
        return id is PersonalStatId.Health or PersonalStatId.Mentality ? (value < 40 ? 2f : 0.3f)
            : Rest.InScope(id) ? (value < 10 ? 1f : 0.1f) : 0f;
    }

    static float Weight(StatId id)
    {
        var cult = GameSession.Cult;
        return id switch
        {
            StatId.Followers => 1f,
            StatId.FollowerFaith => cult[id].Value < 300 ? 0.3f : 0.08f,
            StatId.Reputation => cult[id].Value < 300 ? 0.4f : 0.05f,
            StatId.Offerings => 0.02f,
            StatId.Doctrine => 0.05f,
            _ => 0.02f,
        };
    }

    static float Score(Outcome outcome)
    {
        if (outcome == null) return 0;
        float s = 0;
        foreach (var e in outcome.Effects) s += Weight(e.Stat) * e.Amount;
        foreach (var e in outcome.CultEffects) s += Weight(e.Stat) * e.Amount;
        return s;
    }

    static Choice Best(Question question)
    {
        Choice best = null;
        float bestScore = float.MinValue;
        foreach (var choice in question.Choices)
        {
            var p = choice.Chance >= 1f ? 1f : Math.Max(0.01f, choice.ChanceFor(GameSession.Personal) - GodRules.EventPenalty);
            var score = p * Score(choice.Success) + (1 - p) * Score(choice.Fail);
            if (score > bestScore) { bestScore = score; best = choice; }
        }
        return best;
    }
}

public class GodSimulationTests
{
    // 일반 테스트 실행에는 끼지 않는다. 직접 실행하면 결과를 임시 폴더의 xxxcult_god_report.txt에 남긴다.
    // 같은 실행에서 본편 시뮬레이션도 같은 규모(봇 100명 × 10회차)로 돌려 함께 적는다.
    [Test, Explicit, Timeout(3600000)]
    public void Simulate()
    {
        var bots = int.TryParse(Environment.GetEnvironmentVariable("XXXCULT_SIM_CAREERS"), out var n) ? n : 100;
        var god = GodSimulation.Run(bots, 10, 20261016).Format() + "\n" + GodSimulation.Run(bots, 10, 20261016, GodSimulation.Strategy.Spread).Format()
            + "\n" + GodSimulation.Run(bots, 10, 20261016, GodSimulation.Strategy.Balanced).Format();
        var main = BalanceSimulation.Run(bots, 10, 20261016).Format("본편 (같은 규모)");
        var text = god + "\n" + main;
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "xxxcult_god_report.txt"), text);
        TestContext.WriteLine(text);
    }

    [Test]
    public void SmallSimulation_RunsWithoutErrors()
    {
        var report = GodSimulation.Run(2, 2, 7);
        Assert.AreEqual(4, report.Results.Count);
        Assert.IsFalse(GameSession.GodMode, "시뮬레이션 뒤에는 본편 상태로 돌아가야 함");
    }
}
