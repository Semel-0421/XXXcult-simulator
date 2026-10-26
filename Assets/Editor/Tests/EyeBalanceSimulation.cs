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
//   LowStart: Balanced 계획, 교단을 세운 직후 교주 스탯을 낮춘다 (개인 전부 10, 교리를 뺀 교단 스탯 100)
//   Loser: Balanced 계획, 욕망을 쓰지 않고 이벤트마다 개인 스탯을 가장 많이 잃는(덜 얻는) 선택지를 고른다 (스탯 방치)
//   Exhaust: Balanced 계획, 2년마다 체력을 0으로 만들어 1년 휴식에 들어가게 한다 (휴식 중 재앙 확인용 시나리오)
//   Rester: Balanced 계획이지만 체력·정신력을 욕망으로 채우지 않고 바닥까지 둬 휴식에 들어간다 (스탯 방치 → 배드 경로 확인)
public static class EyeSimulation
{
    public enum Strategy { Balanced, EvilFirst, Wide, Rester, LowStart, Loser, Exhaust }
    public static int MaxYears = 80; // 테스트는 짧게 줄여 돌린다

    public class RunResult
    {
        public Strategy Strategy;
        public EyeSector Sector;
        public string EndingId = "none", Stall = "";
        public int GainEnd, ZeroTurns, Doctrine, DoctrinePeak, Preaches, Proselytes, Years, Calamities, RestCalamities, Learned, PointsEarned, AscendYear = -1;
        public float Dominance, DominancePeak, DemiurgeWeight, PlayerWeight; public int RestTurns, EvilLearned, CounterTotal, SurplusTotal;
    }

    public static List<RunResult> Results = new();
    public static Dictionary<string, int> AchievementBots = new(); // 업적 id -> 그 업적을 딴 봇 수
    public static int Bots;
    public static Dictionary<string, int> Acts = new(); // 행동 분포 (진단용)
    public static Dictionary<string, int> Zeros = new(); // 바닥 스탯별 턴 수 (진단용)

    // ---- 교단 붕괴 원인 집계 (진단용): 구간마다 세 교단 스탯의 증감을 따로 더한다 ----
    static readonly StatId[] Watched = { StatId.Followers, StatId.FollowerFaith, StatId.Reputation, StatId.Doctrine };
    public static Dictionary<string, float[]> Gain = new(), Loss = new(); // 구간 -> 스탯별 합 (Watched 순서)
    public static Dictionary<string, int> Stages = new();                 // "신계 n" / "지상 n" -> 턴 수
    static int[] Snap() => Watched.Select(id => GameSession.Cult[id].Value).ToArray();
    static void Book(string part, int[] before)
    {
        var after = Snap();
        if (!Gain.ContainsKey(part)) { Gain[part] = new float[Watched.Length]; Loss[part] = new float[Watched.Length]; }
        for (int i = 0; i < Watched.Length; i++)
        {
            var d = after[i] - before[i];
            if (d > 0) Gain[part][i] += d; else Loss[part][i] -= d;
        }
    }

    // 압박 피해 추정: 턴이 끝난 뒤의 단계로 GodPressure.ApplyQuarter 식을 다시 계산 (단계는 행동 직후와 거의 같다)
    static readonly System.Reflection.MethodInfo ExposureOf = typeof(GodPressure).GetMethod("Exposure", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
    const float HitDivisor = 3.2f; // GodPressure.HitDivisor와 같게
    static void BookPressure()
    {
        var pressure = GameSession.God.Pressure;
        var cult = GameSession.Cult;
        int heaven = pressure.Stage(Pressure.Heaven, cult), earth = pressure.Stage(Pressure.Earth, cult);
        Stages[$"신계 {heaven}"] = Stages.TryGetValue($"신계 {heaven}", out var h) ? h + 1 : 1;
        Stages[$"지상 {earth}"] = Stages.TryGetValue($"지상 {earth}", out var e) ? e + 1 : 1;
        float Exp(Pressure p) => (float)ExposureOf.Invoke(pressure, new object[] { p });
        var heavenHit = heaven * heaven / HitDivisor * Exp(Pressure.Heaven);
        var earthHit = earth * earth / HitDivisor * Exp(Pressure.Earth);
        const string part = "압박(추정, 행동 턴에 포함)";
        if (!Loss.ContainsKey(part)) { Gain[part] = new float[Watched.Length]; Loss[part] = new float[Watched.Length]; }
        if (heaven > 0)
        {
            Loss[part][0] += cult[StatId.Followers].Value * 0.0255f * heavenHit;
            Loss[part][1] += 13f * heavenHit * (1f - DoctrineLevel.FaithGuard(cult[StatId.Doctrine].Value));
            if (heaven >= 2) Loss[part][3] += 6.5f * heavenHit;
        }
        if (earth > 0) Loss[part][2] += 12f * earthHit;
    }

    public static void Run(int runsPerBot, int seed)
    {
        Results = new List<RunResult>();
        Acts = new Dictionary<string, int>();
        Zeros = new Dictionary<string, int>();
        Gain = new Dictionary<string, float[]>(); Loss = new Dictionary<string, float[]>(); Stages = new Dictionary<string, int>();
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
        Strategy.Balanced or Strategy.Rester or Strategy.LowStart or Strategy.Loser or Strategy.Exhaust => Lines(sector, false).Concat(Climb(sector, 5, 10)).ToList(),
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

    static CultAction ChooseAction(List<AeonSkill> plan, Strategy strategy)
    {
        var me = GameSession.Personal;
        var cult = GameSession.Cult;
        if (strategy is not (Strategy.Rester or Strategy.Loser) && (me[PersonalStatId.Health].Value <= 25 || me[PersonalStatId.Mentality].Value <= 25)) return CultAction.Desire;
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

    // 방치 봇의 이벤트 선택: 개인 스탯 증감의 기대값이 가장 낮은 선택지
    static Choice Worst(Question q)
    {
        float Personal(Outcome o) => o == null ? 0 : o.Effects.Sum(e => (float)e.Amount);
        return q.Choices.OrderBy(c =>
        {
            var p = c.Chance >= 1f ? 1f : c.ChanceFor(GameSession.Personal);
            return p * Personal(c.Success) + (1 - p) * Personal(c.Fail);
        }).First();
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
                r.RestTurns++;
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
        if (strategy == Strategy.LowStart)
        {
            foreach (var id in DemiurgeState.CultStats) if (id != StatId.Doctrine) GameSession.Cult[id].Set(100);
            foreach (var id in DemiurgeState.PersonalStats) GameSession.Personal[id].Set(10);
        }

        var r = new RunResult { Strategy = strategy, Sector = sector };
        var plan = Plan(strategy, sector);
        Endings.Ending ending = null;
        int exhaustedYear = 0;
        while (ending == null && GameSession.CultTime.Years < MaxYears)
        {
            if (strategy == Strategy.Exhaust && GameSession.CultTime.Years >= exhaustedYear + 2)
            {
                exhaustedYear = GameSession.CultTime.Years;
                GameSession.Personal[PersonalStatId.Health].Set(0);
            }
            var snap = Snap();
            Rest(r);
            Book("휴식(휴식 재앙 포함)", snap);
            Learn(plan);
            if ((ending = Progress.DueEnding()) != null) break;
            string key = "consecrate";
            if (!GodSimulation.Consecrate())
            {
                var act = ChooseAction(plan, strategy);
                if (act == CultAction.Preach) r.Preaches++;
                if (act == CultAction.Proselytize) r.Proselytes++;
                snap = Snap();
                Turns.Cult(act);
                Book("행동 턴(행동+사회+불운+분기)", snap);
                var tree = GameSession.God.Aeons;
                r.CounterTotal += EyeRules.CounterGain(tree);
                // 악 우위: 섹터마다 (악·그노시스 노드 수 - 선 노드 수, 최소 0)의 합 — 성장안 계산용
                r.SurplusTotal += EyeRules.Sectors.Sum(sec =>
                {
                    var learned = tree.Learned.Select(AeonSkillCatalog.Find).Where(x => x != null && x.Sector == sec).ToList();
                    return Math.Max(0, learned.Count(x => x.Kind is AeonKind.Evil or AeonKind.Gnosis) - learned.Count(x => x.Kind == AeonKind.Good));
                });
                BookPressure();
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
                snap = Snap();
                Turns.Answer(CalamityChoice(q));
                EyeRules.EndCalamity();
                Book("재앙", snap);
            }
            else
            {
                var next = Turns.NextCultEvent();
                snap = Snap();
                if (next != null) Turns.Answer(strategy == Strategy.Loser ? Worst(next.Question) : GodSimulation.Best(next.Question), next.Id);
                Book("이벤트", snap);
            }
        }
        r.Dominance = EyeRules.Dominance;
        r.DemiurgeWeight = GameSession.God.Demiurge.Weight;
        r.PlayerWeight = DemiurgeState.WeightOf(GameSession.Cult, GameSession.Personal);
        r.EvilLearned = GameSession.God.Aeons.Learned.Count(id => AeonSkillCatalog.Find(id)?.Kind is AeonKind.Evil or AeonKind.Gnosis);
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
        sb.AppendLine("휴식 분기/판 · 휴식 재앙/판 · 승천 순간 교주 무게: " + string.Join(" · ", Results.GroupBy(x => x.Strategy)
            .Select(g => $"{g.Key} {g.Average(x => x.RestTurns):0.0} / {g.Average(x => x.RestCalamities):0.00} / {g.Average(x => x.PlayerWeight):0.00}")));
        sb.AppendLine($"황금의 눈동자 시뮬레이션: 전략 7 × 시작 섹터 4 = 봇 {Bots}명, 판 {Results.Count}개, 최대 {MaxYears}년");
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
        int runs = Math.Max(1, Results.Count);
        sb.AppendLine("교단 스탯 증감 원인 (판당 평균, 오름 / 내림): " + string.Join(" · ", Watched));
        foreach (var part in Loss.Keys)
            sb.AppendLine($"{part} | " + string.Join(" | ", Watched.Select((id, i) => $"+{Gain[part][i] / runs:0} / -{Loss[part][i] / runs:0}")));
        sb.AppendLine("압박 단계 분포(턴): " + string.Join(", ", Stages.OrderBy(x => x.Key).Select(x => $"{x.Key}: {x.Value}")));
        sb.AppendLine("바닥 스탯(턴): " + string.Join(", ", Zeros.OrderByDescending(x => x.Value).Select(x => $"{x.Key} {x.Value}")));
        sb.AppendLine("끝날 때 무게 (데미우르고스 / 교주, 최대 20) · 악·그노시스 노드 수: " + string.Join(" · ", Results.GroupBy(x => x.Strategy)
            .Select(g => $"{g.Key} {g.Average(x => x.DemiurgeWeight):0.00} / {g.Average(x => x.PlayerWeight):0.00} · {g.Average(x => x.EvilLearned):0.0}")));
        sb.AppendLine("누적 카운터 증가 · 누적 악 우위(분기 합) [최소~최대]: " + string.Join(" · ", Results.GroupBy(x => x.Strategy)
            .Select(g => $"{g.Key} {g.Average(x => x.CounterTotal):0} [{g.Min(x => x.CounterTotal)}~{g.Max(x => x.CounterTotal)}] · {g.Average(x => x.SurplusTotal):0} [{g.Min(x => x.SurplusTotal)}~{g.Max(x => x.SurplusTotal)}]")));
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
        Assert.AreEqual(28, EyeSimulation.Results.Count);
        Assert.IsFalse(GameSession.EyeMode, "시뮬레이션 뒤에는 본편 상태로 돌아가야 함");
    }
}
