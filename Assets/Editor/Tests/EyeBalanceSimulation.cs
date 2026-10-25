using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

// 황금의 눈동자 자동 플레이 집계 (SYS_DEMIURGE 9단계). 축성·이벤트 선택은 신 모드 봇(GodSimulation)을 쓰고, 행동은 눈동자 전용 규칙(ChooseAction),
// 아이온 트리 학습 순서와 재앙 선택만 이 모드 것으로 바꾼다. 판마다 승천(엔딩)까지 또는 MaxYears까지 돌린다.
//   Balanced: 시작 섹터의 선·악을 단계마다 번갈아 → 그노시스 → 승천
//   EvilFirst: 시작 섹터의 악한 아이온 4단계까지 먼저, 그다음 선한 아이온 → 그노시스 → 승천
//   Wide: 네 섹터의 0~4단계와 그노시스를 모두 익힌 뒤 시작 섹터로 승천 (네 얼굴을 삼킨 자 노림)
public static class EyeSimulation
{
    public enum Strategy { Balanced, EvilFirst, Wide }
    public static int MaxYears = 80; // 테스트는 짧게 줄여 돌린다

    public class RunResult
    {
        public Strategy Strategy;
        public EyeSector Sector;
        public string EndingId = "none", Stall = "";
        public int GainEnd, ZeroTurns, Doctrine, DoctrinePeak, Preaches, Proselytes, Years, Calamities, RestCalamities, Learned, PointsEarned, AscendYear = -1;
        public float Dominance, DominancePeak;
    }

    public static List<RunResult> Results = new();
    public static Dictionary<string, int> AchievementBots = new(); // 업적 id -> 그 업적을 딴 봇 수
    public static int Bots;
    public static Dictionary<string, int> Acts = new(); // 행동 분포 (진단용)
    public static Dictionary<string, int> Zeros = new(); // 바닥 스탯별 턴 수 (진단용)

    public static void Run(int runsPerBot, int seed)
    {
        Results = new List<RunResult>();
        Acts = new Dictionary<string, int>();
        Zeros = new Dictionary<string, int>();
        AchievementBots = new Dictionary<string, int>();
        Bots = 0;
        var previousPath = Profile.Path;
        var previousEnabled = Profile.Enabled;
        try
        {
            foreach (Strategy strategy in Enum.GetValues(typeof(Strategy)))
                foreach (var sector in EyeRules.Sectors)
                {
                    var temp = Path.Combine(Path.GetTempPath(), $"xxxcult_eye_{Guid.NewGuid():N}.json");
                    Profile.Path = temp;
                    Profile.Enabled = true;
                    var rng = new Random(seed + Bots * 7919);
                    for (int run = 1; run <= runsPerBot; run++) Results.Add(PlayRun(strategy, sector, rng.Next()));
                    foreach (var a in Achievements.All.Where(a => a.Mode == "eye" && Profile.IsUnlocked(a.Id)))
                        AchievementBots[a.Id] = AchievementBots.TryGetValue(a.Id, out var n) ? n + 1 : 1;
                    Bots++;
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
    }

    static IEnumerable<AeonSkill> Lines(EyeSector sector, bool evilFirst)
    {
        if (evilFirst)
        {
            for (int l = 0; l <= 4; l++) yield return AeonSkillCatalog.Of(sector, l, AeonKind.Evil);
            for (int l = 0; l <= 4; l++) yield return AeonSkillCatalog.Of(sector, l, AeonKind.Good);
        }
        else
            for (int l = 0; l <= 4; l++)
            {
                yield return AeonSkillCatalog.Of(sector, l, AeonKind.Good);
                yield return AeonSkillCatalog.Of(sector, l, AeonKind.Evil);
            }
    }

    static IEnumerable<AeonSkill> Climb(EyeSector sector, int from, int to)
    {
        for (int l = from; l <= to; l++) yield return AeonSkillCatalog.Stage(sector, l);
    }

    static List<AeonSkill> Plan(Strategy strategy, EyeSector sector) => strategy switch
    {
        Strategy.Balanced => Lines(sector, false).Concat(Climb(sector, 5, 10)).ToList(),
        Strategy.EvilFirst => Lines(sector, true).Concat(Climb(sector, 5, 10)).ToList(),
        _ => Lines(sector, false).Concat(Climb(sector, 5, 5))
            .Concat(EyeRules.Sectors.Where(s => s != sector).SelectMany(s => Lines(s, false).Concat(Climb(s, 5, 5))))
            .Concat(Climb(sector, 6, 10)).ToList(),
    };

    static int DoctrineLevelNow => DoctrineLevel.Of(GameSession.Cult[StatId.Doctrine].Value);

    static void Learn(List<AeonSkill> plan)
    {
        var god = GameSession.God;
        bool learned;
        do
        {
            learned = false;
            foreach (var skill in plan)
            {
                if (god.Aeons.IsLearned(skill)) continue;
                if (god.Aeons.Learn(skill, god.Available, DoctrineLevelNow)) learned = true;
                break; // 계획 순서를 지킨다: 앞의 노드를 못 익히면 기다린다
            }
        } while (learned);
    }

    // 눈동자 전용 행동: 몸(체력·정신력) → 교감 → 바닥 스탯(0점 벌점) → 교리에 막힌 노드 → 방어선 → 포교
    // 신 봇은 명망·믿음 300을 방어선으로 삼아 봉사만 하게 돼서, 방어선을 낮추고 교리를 앞에 둔다
    public const int Floor = 60, Guard = 150;

    static CultAction ChooseAction(List<AeonSkill> plan)
    {
        var me = GameSession.Personal;
        var cult = GameSession.Cult;
        if (me[PersonalStatId.Health].Value <= 25 || me[PersonalStatId.Mentality].Value <= 25) return CultAction.Desire;
        bool Can(CultAction a) => cult[StatId.Communion].Value >= GodRules.CommunionCost(a);
        if (!Can(CultAction.Proselytize)) return CultAction.Pray;
        if (cult[StatId.Reputation].Value < Floor) return CultAction.Serve;
        if (cult[StatId.FollowerFaith].Value < Floor && Can(CultAction.Preach)) return CultAction.Preach;
        if (cult[StatId.Followers].Value < Floor) return CultAction.Proselytize;
        var next = plan.FirstOrDefault(x => !GameSession.God.Aeons.IsLearned(x));
        bool blocked = next != null && GameSession.God.Aeons.Blocker(next, GameSession.God.Available, DoctrineLevelNow) == "doctrine";
        if (blocked && Can(CultAction.Preach)) return CultAction.Preach;
        if (cult[StatId.Reputation].Value < Guard) return CultAction.Serve;
        if (cult[StatId.FollowerFaith].Value < Guard && Can(CultAction.Preach)) return CultAction.Preach;
        return CultAction.Proselytize;
    }

    // 재앙 선택: 최대치 대비 잃는 비율이 가장 작은 선택지
    static Choice CalamityChoice(Question q)
    {
        float Loss(Outcome o) =>
            o.CultEffects.Sum(e => -e.Amount / (float)ClampedStat.DefaultMax) + o.Effects.Sum(e => -e.Amount / (float)PersonalStatus.MaxValue);
        return q.Choices.OrderBy(c => Loss(c.Success)).First();
    }

    // 휴식: 화면 흐름(CultFlow.RestRoutine)과 같이, 쉬는 중 문턱에 이르면 가장 높은 스탯을 빼앗긴다
    static void Rest(RunResult r)
    {
        for (int guard = 0; guard < 50; guard++)
        {
            var resting = global::Rest.ZeroStats(GameSession.Personal);
            if (resting.Count == 0) return;
            for (int turn = 0; turn < global::Rest.Turns; turn++)
            {
                global::Rest.ApplyTurn(GameSession.Personal, GameSession.Cult, resting);
                GameSession.AdvanceTurn();
                Acts["rest"] = Acts.TryGetValue("rest", out var c) ? c + 1 : 1;
                if (EyeRules.CalamityDue) { EyeRules.RestCalamity(); r.RestCalamities++; }
            }
        }
    }

    static RunResult PlayRun(Strategy strategy, EyeSector sector, int seed)
    {
        EyeRules.Begin(seed);
        GameSession.Personal.Gender = seed % 2 == 0 ? Gender.Male : Gender.Female;
        EyeRules.ChooseSector(sector);
        GameSession.MarkStarted();
        GameSession.FoundCult("시뮬눈동자");
        EyeRules.OnFounded();

        var r = new RunResult { Strategy = strategy, Sector = sector };
        var plan = Plan(strategy, sector);
        Endings.Ending ending = null;
        while (ending == null && GameSession.CultTime.Years < MaxYears)
        {
            Rest(r);
            Learn(plan);
            if ((ending = Progress.DueEnding()) != null) break;
            string key = "consecrate";
            if (!GodSimulation.Consecrate())
            {
                var act = ChooseAction(plan);
                if (act == CultAction.Preach) r.Preaches++;
                if (act == CultAction.Proselytize) r.Proselytes++;
                Turns.Cult(act);
                key = act.ToString();
            }
            Acts[key] = Acts.TryGetValue(key, out var c) ? c + 1 : 1;
            var cult = GameSession.Cult; var me = GameSession.Personal;
            var zero = new List<string>();
            foreach (var id in new[] { StatId.Followers, StatId.FollowerFaith, StatId.Reputation }) if (cult[id].Value <= 1) zero.Add(id.ToString());
            foreach (var id in new[] { PersonalStatId.Health, PersonalStatId.Mentality }) if (me[id].Value <= 0) zero.Add(id.ToString());
            if (zero.Count > 0) r.ZeroTurns++;
            foreach (var z in zero) Zeros[z] = Zeros.TryGetValue(z, out var zc) ? zc + 1 : 1;
            r.DoctrinePeak = Math.Max(r.DoctrinePeak, GameSession.Cult[StatId.Doctrine].Value);
            r.DominancePeak = Math.Max(r.DominancePeak, EyeRules.Dominance);
            if (EyeRules.CalamityDue)
            {
                var q = EyeRules.BeginCalamity();
                Turns.Answer(CalamityChoice(q));
                EyeRules.EndCalamity();
            }
            else
            {
                var next = Turns.NextCultEvent();
                if (next != null) Turns.Answer(GodSimulation.Best(next.Question), next.Id);
            }
        }
        r.Dominance = EyeRules.Dominance;
        if (ending != null) { Progress.Reach(ending); r.EndingId = ending.Id; r.AscendYear = GameSession.CultTime.Years; }
        r.Years = GameSession.CultTime.Years;
        r.Calamities = GameSession.God.Demiurge.Calamities;
        r.Learned = GameSession.God.Aeons.Learned.Count();
        r.PointsEarned = GameSession.God.Points.Earned;
        r.Doctrine = DoctrineLevelNow;
        r.GainEnd = EyeRules.CounterGain(GameSession.God.Aeons);
        var stuck = plan.FirstOrDefault(x => !GameSession.God.Aeons.IsLearned(x));
        if (stuck != null) r.Stall = stuck.Id + ":" + GameSession.God.Aeons.Blocker(stuck, GameSession.God.Available, DoctrineLevelNow);
        return r;
    }

    static string Pct(int n, int total) => total == 0 ? "-" : $"{100f * n / total:0}%";

    static float Median(IEnumerable<int> values)
    {
        var v = values.OrderBy(x => x).ToList();
        return v.Count == 0 ? -1 : v.Count % 2 == 1 ? v[v.Count / 2] : (v[v.Count / 2 - 1] + v[v.Count / 2]) / 2f;
    }

    public static string Format()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"황금의 눈동자 시뮬레이션: 전략 3 × 시작 섹터 4 = 봇 {Bots}명, 판 {Results.Count}개, 최대 {MaxYears}년");
        sb.AppendLine("전략 | 판 | 승천 | 승천 연수(중앙값) | 굿 | 배드 | 다른 눈동자 | 네 얼굴 | 재앙/판 | 휴식 재앙/판 | 최종 주도권(평균) | 주도권 최고(평균)");
        foreach (var group in Results.GroupBy(x => x.Strategy))
        {
            var g = group.ToList();
            var asc = g.Where(x => x.AscendYear >= 0).ToList();
            sb.AppendLine($"{group.Key} | {g.Count} | {Pct(asc.Count, g.Count)} | {Median(asc.Select(x => x.AscendYear))} | "
                + $"{Pct(g.Count(x => x.EndingId.EndsWith("_good")), g.Count)} | {Pct(g.Count(x => x.EndingId.EndsWith("_bad")), g.Count)} | "
                + $"{Pct(g.Count(x => x.EndingId.EndsWith("_other")), g.Count)} | {Pct(g.Count(x => x.EndingId == "eye_four_faces"), g.Count)} | "
                + $"{g.Average(x => x.Calamities):0.0} | {g.Average(x => x.RestCalamities):0.0} | {g.Average(x => x.Dominance):0.00} | {g.Average(x => x.DominancePeak):0.00}");
        }
        sb.AppendLine();
        sb.AppendLine("전략 | 익힌 노드(평균) | 번 포인트(평균) | 교리 단계(평균) | 교리 최고값 | 설교/포교 | 끝 분기 카운터 | 바닥 턴 | 막힌 곳(상위)");
        foreach (var group in Results.GroupBy(x => x.Strategy))
            sb.AppendLine($"{group.Key} | {group.Average(x => x.Learned):0.0} | {group.Average(x => x.PointsEarned):0} | {group.Average(x => x.Doctrine):0.0} | {group.Average(x => x.DoctrinePeak):0} | {group.Average(x => x.Preaches):0}/{group.Average(x => x.Proselytes):0} | {group.Average(x => x.GainEnd):0.0} | {group.Average(x => x.ZeroTurns):0} | "
                + string.Join(", ", group.GroupBy(x => x.Stall).OrderByDescending(x => x.Count()).Take(4).Select(x => $"{x.Key}×{x.Count()}")));
        sb.AppendLine();
        sb.AppendLine("바닥 스탯(턴): " + string.Join(", ", Zeros.OrderByDescending(x => x.Value).Select(x => $"{x.Key} {x.Value}")));
        sb.AppendLine("행동 분포(전체 턴): " + string.Join(", ", Acts.OrderByDescending(x => x.Value).Select(x => $"{x.Key} {x.Value}")));
        sb.AppendLine();
        sb.AppendLine("섹터 | 판 | 승천 | 승천 연수(중앙값) | 배드");
        foreach (var group in Results.GroupBy(x => x.Sector))
        {
            var g = group.ToList();
            var asc = g.Where(x => x.AscendYear >= 0).ToList();
            sb.AppendLine($"{group.Key} | {g.Count} | {Pct(asc.Count, g.Count)} | {Median(asc.Select(x => x.AscendYear))} | {Pct(g.Count(x => x.EndingId.EndsWith("_bad")), g.Count)}");
        }
        sb.AppendLine();
        sb.AppendLine("엔딩 | 판");
        foreach (var e in Results.GroupBy(x => x.EndingId).OrderByDescending(x => x.Count())) sb.AppendLine($"{e.Key} | {e.Count()}");
        sb.AppendLine();
        sb.AppendLine($"업적 도달(봇 {Bots}명 중 딴 봇 수) — 0명인 업적");
        foreach (var a in Achievements.All.Where(a => a.Mode == "eye" && !AchievementBots.ContainsKey(a.Id))) sb.AppendLine($"{a.Id} {a.Name} | {a.ConditionText}");
        sb.AppendLine();
        sb.AppendLine("업적 도달 — 모든 봇이 딴 업적");
        foreach (var a in Achievements.All.Where(a => a.Mode == "eye" && AchievementBots.TryGetValue(a.Id, out var n) && n == Bots)) sb.AppendLine($"{a.Id} {a.Name}");
        return sb.ToString();
    }
}

public class EyeSimulationTests
{
    [Test]
    public void SmallSimulation_RunsWithoutErrors()
    {
        var years = EyeSimulation.MaxYears;
        EyeSimulation.MaxYears = 2;
        try { EyeSimulation.Run(1, 7); }
        finally { EyeSimulation.MaxYears = years; }
        Assert.AreEqual(12, EyeSimulation.Results.Count);
        Assert.IsFalse(GameSession.EyeMode, "시뮬레이션 뒤에는 본편 상태로 돌아가야 함");
    }
}
