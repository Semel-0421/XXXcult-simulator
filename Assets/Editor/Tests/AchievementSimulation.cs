using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

// 업적 도달률 시뮬레이션: 가상 플레이어가 한 모드를 K판 이어 한다. 업적·카운터는 플레이어 단위로 누적한다 (메모리 전용 프로필, 디스크 안 씀).
// 판정(Achievements.Evaluate: 턴·선택·교리·엔딩)마다 v2 업적 조건을 전부 확인해 그 판에 한 번이라도 충족한 업적을 기록한다.
// 배치(RunBatch)는 원자료를 Temp/ach_raw_<mode>.tsv에 덧붙이고, WriteReport가 Temp/ach_sim_<mode>.txt로 모은다.
// 원자료 한 줄 = 한 판: tag, style, player, run, ending, outcome, years, 충족한 업적 id(쉼표)
public static class AchievementSimulation
{
    public enum SimMode { Normal, Mirror, God }
    public enum Style { Desire, Virtue, Balanced }

    static readonly DoctrineBranch[] Branches =
    {
        DoctrineBranch.Lust, DoctrineBranch.Greed, DoctrineBranch.Appetite, DoctrineBranch.Ambition,
        DoctrineBranch.Temperance, DoctrineBranch.Charity, DoctrineBranch.Patience, DoctrineBranch.Integrity,
    };
    static readonly GodBranch[] GodAll = { GodBranch.Miracle, GodBranch.Prophecy, GodBranch.Mercy, GodBranch.Desire };

    static string TempDir => Path.Combine(UnityEngine.Application.dataPath, "..", "Temp");
    public static string RawPath(SimMode mode) => Path.Combine(TempDir, $"ach_raw_{mode.ToString().ToLowerInvariant()}.tsv");
    public static string ReportPath(SimMode mode) => Path.Combine(TempDir, $"ach_sim_{mode.ToString().ToLowerInvariant()}.txt");

    public static string KeysPath(SimMode mode) => Path.Combine(TempDir, $"ach_keys_{mode.ToString().ToLowerInvariant()}.tsv");

    static readonly List<Achievement> V2 = AchievementCatalogV2.All.ToList();
    static HashSet<string> runMet;
    static Dictionary<string, int> peakMax, peakMin; // 이 플레이어가 지금까지 본 조건 값의 최고·최저 (기준치 제안용)

    // 조건 토큰마다 지금 값을 읽는 함수 (AchievementCondition과 같은 규칙). 값이 아직 없으면 null
    static readonly Regex Op = new(@"^(?<k>[A-Za-z0-9_:.]+?)(>=|<=|==)");
    static List<(string key, Func<int?> read)> readers;
    static List<(string key, Func<int?> read)> Readers => readers ??= V2.SelectMany(a => a.ConditionText.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        .Select(t => Op.Match(t) is { Success: true } m ? m.Groups["k"].Value : t)
        .Where(k => k != "founded" && k != "declared" && k != "standing").Distinct().Select(k => (k, Reader(k))).ToList();

    static Func<int?> Reader(string key)
    {
        if (key.StartsWith("NEW:")) key = key.Substring(4);
        if (key.StartsWith("c:")) { var n = key.Substring(2); return () => GameSession.CountOf(n); }
        if (key.StartsWith("p:")) { var st = (PersonalStatId)Enum.Parse(typeof(PersonalStatId), key.Substring(2)); return () => GameSession.Personal[st].Value; }
        if (key.StartsWith("s:")) { var st = (StatId)Enum.Parse(typeof(StatId), key.Substring(2)); return () => GameSession.CultFounded ? GameSession.Cult[st].Value : (int?)null; }
        if (key.StartsWith("profile:")) { var n = key.Substring(8); return () => Profile.Has(n) ? Profile.CountOf(n) : Counters.Has(n) ? Counters.Read(n) : (int?)null; }
        switch (key)
        {
            case "age": return () => GameSession.Age.Years;
            case "cultyears": return () => GameSession.CultFounded ? GameSession.CultTime.Years : (int?)null;
            case "doctrine": return () => DoctrineLevel.Of(GameSession.Cult[StatId.Doctrine].Value);
            case "gifted": return () => GameSession.Personal.Talents.Owned.Count;
            case "achv": return () => Achievements.CountedUnlocked();
        }
        if (key.EndsWith("_achv")) { var m = key.Substring(0, key.Length - 5); return () => Achievements.CountedUnlocked(m); }
        return () => Counters.Has(key) ? Counters.Read(key) : (int?)null;
    }

    // 배치 하나를 돌려 원자료 파일에 덧붙인다. gift: null 실제 규칙(연결 업적 달성 시 보유), true 전부 보유, false 하나도 없음
    public static string RunBatch(SimMode mode, Style style, int players, int runs, int seed, bool? gift = null, string tag = "base")
    {
        var keys = new StringBuilder();
        var text = Collect(mode, style, players, runs, seed, gift, tag, keys);
        File.AppendAllText(RawPath(mode), text, new UTF8Encoding(false));
        File.AppendAllText(KeysPath(mode), keys.ToString(), new UTF8Encoding(false));
        return $"ok {tag} {mode} {style} {players}x{runs}";
    }

    public static string Collect(SimMode mode, Style style, int players, int runs, int seed, bool? gift = null, string tag = "base", StringBuilder keys = null)
    {
        var sb = new StringBuilder();
        var prevPath = Profile.Path;
        var prevEnabled = Profile.Enabled;
        Profile.InMemory = true;
        Profile.Path = Path.Combine(Path.GetTempPath(), "xxxcult_achsim_unused.json"); // 메모리 전용이라 읽지도 쓰지도 않음
        Profile.Enabled = true;
        Achievements.ForceGiftedV2 = gift;
        Achievements.Evaluated += OnEvaluated;
        try
        {
            for (int p = 0; p < players; p++)
            {
                Profile.Reset(); // 새 플레이어: 업적·누적 카운터·본 엔딩을 비운다 (판 사이에는 유지)
                var rng = new Random(seed + p * 7919);
                BalanceSimulation.pathRng = new Random(rng.Next());
                peakMax = new Dictionary<string, int>();
                peakMin = new Dictionary<string, int>();
                for (int r = 1; r <= runs; r++)
                {
                    runMet = new HashSet<string>();
                    var ending = Play(mode, style, r, rng);
                    OnEvaluated(); // 엔딩 없이 끝난 판의 마지막 상태도 한 번 더 본다
                    sb.Append(tag).Append('\t').Append(style).Append('\t').Append($"{seed}-{p}").Append('\t').Append(r).Append('\t')
                      .Append(ending?.Id ?? "none").Append('\t').Append(Outcome(mode, ending)).Append('\t').Append(GameSession.CultTime.Years).Append('\t')
                      .AppendLine(string.Join(",", runMet));
                }
                foreach (var pair in peakMax)
                    keys?.Append(tag).Append('\t').Append(style).Append('\t').Append($"{seed}-{p}").Append('\t').Append(pair.Key).Append('\t')
                        .Append(pair.Value).Append('\t').Append(peakMin[pair.Key]).Append('\n');
            }
        }
        finally
        {
            Achievements.Evaluated -= OnEvaluated;
            Achievements.ForceGiftedV2 = null;
            runMet = null;
            peakMax = peakMin = null;
            BalanceSimulation.pathRng = null;
            BalanceSimulation.mirrorOn = false;
            BalanceSimulation.neutralBot = false;
            Profile.Enabled = prevEnabled;
            Profile.InMemory = false;
            Profile.Path = prevPath; // Reset 포함
            GameSession.StartNew(1);
        }
        return sb.ToString();
    }

    static void OnEvaluated()
    {
        if (runMet == null) return;
        foreach (var a in V2)
            if (!runMet.Contains(a.Id) && a.IsMet()) runMet.Add(a.Id);
        if (peakMax == null) return;
        foreach (var (key, read) in Readers)
        {
            if (read() is not int v) continue;
            if (!peakMax.TryGetValue(key, out var hi) || v > hi) peakMax[key] = v;
            if (!peakMin.TryGetValue(key, out var lo) || v < lo) peakMin[key] = v;
        }
    }

    static Endings.Ending Play(SimMode mode, Style style, int run, Random rng)
    {
        if (mode == SimMode.God)
        {
            var pool = style == Style.Desire ? new[] { GodBranch.Desire, GodBranch.Miracle }
                : style == Style.Virtue ? new[] { GodBranch.Mercy, GodBranch.Prophecy } : GodAll;
            var first = pool[rng.Next(pool.Length)];
            var second = pool.Where(x => x != first).ElementAt(rng.Next(pool.Length - 1));
            var strategy = (GodSimulation.Strategy)rng.Next(3); // 핵 루트·분산·절충을 섞는다 (실제 플레이어처럼 판마다 다르게)
            var result = GodSimulation.PlayRun(run, first, second, rng.Next(), strategy);
            return GodRules.Find(result.EndingId);
        }
        BalanceSimulation.mirrorOn = mode == SimMode.Mirror;
        BalanceSimulation.neutralBot = style == Style.Balanced;
        BalanceSimulation.neutralPick = null;
        bool desireSide = style == Style.Desire || (style == Style.Balanced && rng.Next(2) == 0);
        var branches = Branches.Where(b => Endings.IsDesireBranch(b) == desireSide).ToArray();
        var goal = new BalanceSimulation.Goal(branches[rng.Next(branches.Length)], rng.Next(2) == 0 ? EndingKind.Good : EndingKind.Bad);
        return BalanceSimulation.PlayRun(run, goal, rng.Next(), new BalanceSimulation.Report());
    }

    // 결과 분류: collapse(교단 붕괴·해산), final(최종 스킬 엔딩), ascend(신 승천), death(사망·노환), defeat(신 패배), other, none(엔딩 없음)
    static string Outcome(SimMode mode, Endings.Ending e)
    {
        if (e == null) return "none";
        switch (mode)
        {
            case SimMode.God: return GodRules.IsAscension(e) ? "ascend" : "defeat";
            case SimMode.Mirror:
                if (e.Id is "inv_end_absorbed" or "inv_end_followers") return "collapse";
                if (e.Id.StartsWith(MirrorRules.FinalPrefix)) return "final";
                return Progress.IsDeath(e) ? "death" : "other";
            default:
                if (e.Id == Endings.Dissolved) return "collapse";
                if (e.Final != null) return "final";
                return Progress.IsDeath(e) ? "death" : "other";
        }
    }

    // ---- 집계 ----
    sealed class Row { public string Tag, Style, Player, Ending, Outcome; public int Run, Years; public string[] Met; }

    static List<Row> ReadRaw(SimMode mode) =>
        File.Exists(RawPath(mode))
            ? File.ReadAllLines(RawPath(mode)).Where(l => l.Length > 0).Select(l => l.Split('\t')).Select(f => new Row
            {
                Tag = f[0], Style = f[1], Player = f[2], Run = int.Parse(f[3]), Ending = f[4], Outcome = f[5], Years = int.Parse(f[6]),
                Met = f.Length > 7 && f[7].Length > 0 ? f[7].Split(',') : Array.Empty<string>(),
            }).ToList()
            : new List<Row>();

    static string Pct(int n, int total) => total == 0 ? "-" : $"{100.0 * n / total:0.0}%";

    static string OutcomeLine(string label, List<Row> rows)
    {
        int n = rows.Count;
        string Of(string o) => Pct(rows.Count(r => r.Outcome == o), n);
        return $"  {label,-22} 판 {n,5}: 붕괴 {Of("collapse"),6} 최종스킬 {Of("final"),6} 승천 {Of("ascend"),6} 사망 {Of("death"),6} 패배 {Of("defeat"),6} 기타 {Of("other"),6} 없음 {Of("none"),6} / 평균 {(n == 0 ? 0 : rows.Average(r => r.Years)):0.0}년";
    }

    // 유효숫자 두 자리로 반올림 (기준치 제안용)
    static int Nice(int v)
    {
        if (Math.Abs(v) < 20) return v;
        var mag = (int)Math.Pow(10, (int)Math.Floor(Math.Log10(Math.Abs(v))) - 1);
        return (int)Math.Round((double)v / mag) * mag;
    }

    public static string WriteReport(SimMode mode)
    {
        var rows = ReadRaw(mode);
        var baseRows = rows.Where(r => r.Tag == "base").ToList();
        var styles = baseRows.Select(r => r.Style).Distinct().OrderBy(s => s).ToList();
        var players = baseRows.GroupBy(r => r.Style + "|" + r.Player).ToList();
        var maxRun = baseRows.Count == 0 ? 0 : baseRows.Max(r => r.Run);
        var modeName = mode.ToString().ToLowerInvariant();

        // 플레이어별 충족 업적 + 첫 충족 판
        var playerFirst = players.ToDictionary(g => g.Key, g =>
        {
            var d = new Dictionary<string, int>();
            foreach (var r in g.OrderBy(r => r.Run)) foreach (var id in r.Met) if (!d.ContainsKey(id)) d[id] = r.Run;
            return d;
        });
        var runCount = baseRows.SelectMany(r => r.Met).GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());

        double PlayerRate(string id, string style = null)
        {
            var ps = players.Where(g => style == null || g.Key.StartsWith(style + "|")).ToList();
            return ps.Count == 0 ? 0 : 100.0 * ps.Count(g => playerFirst[g.Key].ContainsKey(id)) / ps.Count;
        }
        double RunRate(string id) => baseRows.Count == 0 ? 0 : 100.0 * (runCount.TryGetValue(id, out var n) ? n : 0) / baseRows.Count;
        double MedianFirst(string id)
        {
            var xs = playerFirst.Values.Where(d => d.ContainsKey(id)).Select(d => d[id]).OrderBy(x => x).ToList();
            return xs.Count == 0 ? 0 : xs[xs.Count / 2];
        }

        var sb = new StringBuilder();
        sb.AppendLine($"[업적 도달률 · {modeName}] 플레이어 {players.Count}명 (스타일 {string.Join("/", styles.Select(s => $"{s} {players.Count(g => g.Key.StartsWith(s + "|"))}"))}) × 최대 {maxRun}판 = {baseRows.Count}판");
        sb.AppendLine("  도달률: 플레이어% = K판 안에 한 번이라도 조건을 충족한 플레이어 비율, 판% = 조건을 충족한 판 비율, 첫판 = 처음 충족한 판 번호 중앙값");
        sb.AppendLine("결과 분포 (스타일별, 실제 보유 규칙):");
        foreach (var s in styles) sb.AppendLine(OutcomeLine(s, baseRows.Where(r => r.Style == s).ToList()));
        sb.AppendLine(OutcomeLine("전체", baseRows));
        foreach (var tag in rows.Select(r => r.Tag).Where(t => t != "base").Distinct().OrderBy(t => t))
            sb.AppendLine(OutcomeLine(tag, rows.Where(r => r.Tag == tag).ToList()));

        string Line(Achievement a) =>
            $"  {a.Id} t{a.Tier}{(a.Hidden ? " H" : "")}{(a.Adult ? " A" : "")} | 플레이어 {PlayerRate(a.Id),5:0.0}% [{string.Join(" ", styles.Select(s => $"{s[0]}{PlayerRate(a.Id, s):0}"))}] | 판 {RunRate(a.Id),5:0.0}% | 첫판 {MedianFirst(a.Id),2:0} | {a.ConditionText} | {a.Name}";

        var own = V2.Where(a => a.Mode == modeName).ToList();
        sb.AppendLine($"\n== 이 모드 업적 ({own.Count}개) ==");
        foreach (var a in own) sb.AppendLine(Line(a));
        sb.AppendLine("\n== 히든 업적 ==");
        foreach (var a in V2.Where(a => a.Mode == "hidden")) sb.AppendLine(Line(a));

        var zero = own.Where(a => PlayerRate(a.Id) == 0).ToList();
        sb.AppendLine($"\n== 0% (모든 스타일, 이 모드 업적 {zero.Count}개) ==");
        foreach (var a in zero) sb.AppendLine($"  {a.Id} t{a.Tier} | {a.ConditionText}");
        var easy = own.Where(a => PlayerRate(a.Id) > 90).OrderByDescending(a => PlayerRate(a.Id)).ToList();
        sb.AppendLine($"\n== 90% 초과 ({easy.Count}개) ==");
        foreach (var a in easy) sb.AppendLine($"  {a.Id} t{a.Tier} {PlayerRate(a.Id):0.0}% (첫판 {MedianFirst(a.Id):0}) | {a.ConditionText}");
        var other = V2.Where(a => a.Mode != modeName && a.Mode != "hidden" && PlayerRate(a.Id) > 0).ToList();
        sb.AppendLine($"\n== 다른 모드 업적 중 이 모드에서 충족된 것 ({other.Count}개) ==");
        foreach (var a in other) sb.AppendLine($"  {a.Id} ({a.Mode}) {PlayerRate(a.Id):0.0}% | {a.ConditionText}");

        // 조건 값 분포와 기준치 제안: 플레이어별 최고값(<=는 최저값)의 분위수에서 티어 목표 도달률(t1 60%, t2 30%, t3·t4 8%, 히든 5%)에 맞는 값
        var keyRows = File.Exists(KeysPath(mode)) ? File.ReadAllLines(KeysPath(mode)).Select(l => l.Split('\t')).Where(f => f.Length == 6 && f[0] == "base").ToList() : new List<string[]>();
        var byKey = keyRows.GroupBy(f => f[3]).ToDictionary(g => g.Key, g => g.ToList());
        int total = players.Count;
        int Q(List<int> xs, double f) => xs.Count == 0 ? 0 : xs[(int)Math.Floor(f * (xs.Count - 1))];
        sb.AppendLine("\n== 조건 값 분포 (플레이어별 최고값: p10 p50 p90 최대 / 값을 기록한 플레이어 수) ==");
        foreach (var pair in byKey.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            var xs = pair.Value.Select(f => int.Parse(f[4])).Concat(Enumerable.Repeat(0, Math.Max(0, total - pair.Value.Count))).OrderBy(x => x).ToList();
            sb.AppendLine($"  {pair.Key}: {Q(xs, .1)} {Q(xs, .5)} {Q(xs, .9)} {xs.LastOrDefault()} / {pair.Value.Count}명");
        }
        sb.AppendLine("\n== 기준치 제안 (단일 수치 조건, 현재 도달률이 목표 밖인 것) ==");
        var single = new Regex(@"^(?:NEW:)?(?<k>[A-Za-z0-9_:.]+?)(?<op>>=|<=)(?<n>-?\d+)$");
        foreach (var a in own.Concat(V2.Where(a => a.Mode == "hidden")))
        {
            var m = single.Match(a.ConditionText);
            if (!m.Success) continue;
            double target = a.Hidden ? 0.05 : a.Tier == 1 ? 0.6 : a.Tier == 2 ? 0.3 : 0.08;
            double lo = a.Hidden ? 0.5 : a.Tier == 1 ? 40 : a.Tier == 2 ? 10 : 1, hi = a.Hidden ? 10 : a.Tier == 1 ? 80 : a.Tier == 2 ? 50 : 15;
            var rate = PlayerRate(a.Id);
            if (rate >= lo && rate <= hi) continue;
            var key = m.Groups["k"].Value;
            if (!byKey.TryGetValue(key, out var vals) && !byKey.TryGetValue("NEW:" + key, out vals)) { sb.AppendLine($"  {a.Id} t{a.Tier} {rate:0.0}% | {a.ConditionText} | 값 기록 없음"); continue; }
            bool atLeast = m.Groups["op"].Value == ">=";
            string suggestion;
            if (atLeast)
            {
                var xs = vals.Select(f => int.Parse(f[4])).Concat(Enumerable.Repeat(0, Math.Max(0, total - vals.Count))).OrderBy(x => x).ToList();
                var t = Q(xs, 1 - target);
                suggestion = t <= 0 ? "목표 도달률을 만들 수 없음(대부분 0)" : $">={Nice(t)}";
            }
            else
            {
                var xs = vals.Select(f => int.Parse(f[5])).OrderBy(x => x).ToList();
                suggestion = xs.Count < total * target ? "목표 도달률을 만들 수 없음(값 기록이 적음)" : $"<={Nice(Q(xs.Concat(Enumerable.Repeat(int.MaxValue, total - xs.Count)).OrderBy(x => x).ToList(), target))}";
            }
            sb.AppendLine($"  {a.Id} t{a.Tier}{(a.Hidden ? " H" : "")} {rate:0.0}% (목표 {lo}~{hi}%) | {a.ConditionText} → {suggestion}");
        }

        // 기계 판독용 (보고서 표 만들 때)
        sb.AppendLine("\n== TSV id\tplayer%\trun%\tmedianFirst ==");
        foreach (var a in V2) sb.AppendLine($"{a.Id}\t{PlayerRate(a.Id):0.0}\t{RunRate(a.Id):0.0}\t{MedianFirst(a.Id):0}");

        File.WriteAllText(ReportPath(mode), sb.ToString(), new UTF8Encoding(false));
        return "ok " + ReportPath(mode);
    }
}

public class AchievementSimulationTests
{
    // 일반 테스트 실행에는 끼지 않는다: 모드·스타일마다 플레이어 60명 × 20판 + 기프티드 전부/없음 비교 (오래 걸림)
    [Test, Explicit("수동 집계용: 수천 판을 돌려 오래 걸린다")]
    public void Simulate()
    {
        foreach (AchievementSimulation.SimMode mode in Enum.GetValues(typeof(AchievementSimulation.SimMode)))
        {
            if (File.Exists(AchievementSimulation.RawPath(mode))) File.Delete(AchievementSimulation.RawPath(mode));
            if (File.Exists(AchievementSimulation.KeysPath(mode))) File.Delete(AchievementSimulation.KeysPath(mode));
            foreach (AchievementSimulation.Style style in Enum.GetValues(typeof(AchievementSimulation.Style)))
                AchievementSimulation.RunBatch(mode, style, 60, 20, 20261020 + (int)style * 1000 + (int)mode * 100000);
            AchievementSimulation.RunBatch(mode, AchievementSimulation.Style.Balanced, 20, 20, 777, true, "gift_all");
            AchievementSimulation.RunBatch(mode, AchievementSimulation.Style.Balanced, 20, 20, 777, false, "gift_none");
            AchievementSimulation.WriteReport(mode);
        }
        Assert.Pass();
    }

    [Test]
    public void SmallCollect_RestoresProfileState()
    {
        var enabled = Profile.Enabled;
        var lines = AchievementSimulation.Collect(AchievementSimulation.SimMode.Normal, AchievementSimulation.Style.Balanced, 1, 2, 7)
            .Split('\n').Count(l => l.Trim().Length > 0);
        Assert.AreEqual(2, lines);
        Assert.IsFalse(Profile.InMemory);
        Assert.IsNull(Achievements.ForceGiftedV2);
        Assert.AreEqual(enabled, Profile.Enabled);
    }
}
