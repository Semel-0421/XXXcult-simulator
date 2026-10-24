using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

// 업적 조건 문자열을 해석해 현재 판 상태로 달성 여부를 판단한다. 공백으로 나눈 조건은 모두 만족해야 한다.
//   founded / declared                 교단 창설 / 교리 선포를 익힘
//   age>=N  cultyears>=N               나이 / 교단 창설 후 햇수
//   doctrine>=N  gifted>=N  achv>=N    교리 레벨 / 보유 기프티드 수 / 달성 업적 수
//   c:KEY>=N                           이번 판 카운터 (events, choice_ok, act_Preach ...)
//   p:Stat>=N  s:Stat>=N               개인 스탯 / 교단 스탯 값 (s는 교단 창설 후에만)
//   standing>=Name  standing<=Name     사회적 시선 단계 (교단 창설 후에만)
//   profile:KEY>=N                     여러 판에 걸친 카운터 (runs, dissolved, founded). Profile에 없는 키는 카운터의 누적(Profile) 범위로 읽음
//   NEW:KEY>=N  NEW:KEY<=N  NEW:KEY==N Counters.Read(KEY) 비교. NEW: 뒤 토큰은 접두어 없이 '_' 가 든 이름이면 카운터 키로 봄
//   KEY (연산자 없음)                  카운터 KEY >= 1
//   TOKEN for Ny                       TOKEN 이 N년 연속 유지됨: 카운터 "hold:TOKEN" >= N (게임 쪽이 해마다 올리고 깨지면 0으로 Set)
//   {normal|god|mirror}_achv            그 모드의 달성 업적 수 (히든·성인 제외)
public class AchievementCondition
{
    static readonly Regex TokenPattern = new(@"^(?<key>[A-Za-z0-9_:.]+)(?<op>>=|<=)(?<value>[A-Za-z0-9_]+)$", RegexOptions.Compiled);

    readonly List<Func<bool>> checks = new();

    public static AchievementCondition Parse(string text)
    {
        var condition = new AchievementCondition();
        var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < tokens.Length; i++)
        {
            if (tokens[i] == "for" && i > 0 && i + 1 < tokens.Length && Regex.IsMatch(tokens[i + 1], @"^\d+y$"))
            {
                var key = "hold:" + StripNew(tokens[i - 1]);
                var years = int.Parse(tokens[i + 1].TrimEnd('y'));
                condition.checks[condition.checks.Count - 1] = () => Counters.Read(key) >= years;
                i++;
                continue;
            }
            condition.checks.Add(ParseToken(tokens[i], text));
        }
        if (condition.checks.Count == 0) throw new ArgumentException("빈 업적 조건");
        return condition;
    }

    public bool IsMet()
    {
        foreach (var check in checks)
            if (!check()) return false;
        return true;
    }

    static string StripNew(string token) => token.StartsWith("NEW:") ? token.Substring(4) : token;

    // 조건 문자열에서 참조하는 카운터 키 목록 추출 (역인덱스용)
    public static IReadOnlyList<string> ExtractCounterKeys(string conditionText)
    {
        var keys = new List<string>();
        var tokens = conditionText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < tokens.Length; i++)
        {
            if (tokens[i] == "for" && i > 0 && i + 1 < tokens.Length && Regex.IsMatch(tokens[i + 1], @"^\d+y$"))
            {
                keys.Add("hold:" + StripNew(tokens[i - 1]));
                i++;
                continue;
            }
            var token = tokens[i];
            if (!IsCounterToken(token)) continue;
            var m = CounterPattern.Match(StripNew(token));
            if (m.Success) keys.Add(m.Groups["key"].Value);
        }
        return keys;
    }

    static readonly Regex CounterPattern = new(@"^(?<key>[A-Za-z0-9_.]+)(?:(?<op>>=|<=|==)(?<value>-?\d+))?$", RegexOptions.Compiled);
    static readonly HashSet<string> OldKeys = new() { "age", "cultyears", "doctrine", "gifted", "achv", "standing" };

    // 기존 토큰이 아니면 카운터 토큰으로 본다 (NEW: 접두어가 있거나, '_' 가 든 이름이거나)
    static bool IsCounterToken(string token)
    {
        if (token.StartsWith("NEW:")) return true;
        if (token == "founded" || token == "declared") return false;
        var key = token.Split(new[] { '>', '<', '=' })[0];
        if (OldKeys.Contains(key) || key.Contains(':')) return false;
        return key.Contains('_');
    }

    static int ReadCounter(string key)
    {
        if (key.EndsWith("_achv")) return Achievements.CountedUnlocked(key.Substring(0, key.Length - 5));
        return Counters.Read(key);
    }

    static Func<bool> ParseCounter(string token, string whole)
    {
        var m = CounterPattern.Match(StripNew(token));
        if (!m.Success) throw new ArgumentException($"잘못된 업적 조건 '{token}' ({whole})");
        var key = m.Groups["key"].Value;
        if (!m.Groups["op"].Success) return () => ReadCounter(key) >= 1;
        var n = int.Parse(m.Groups["value"].Value);
        switch (m.Groups["op"].Value)
        {
            case ">=": return () => ReadCounter(key) >= n;
            case "<=": return () => Counters.Has(key) && ReadCounter(key) <= n; // 아직 세지 않은 값은 미달성
            default: return () => Counters.Has(key) && ReadCounter(key) == n;
        }
    }

    static Func<bool> CompareProfile(string name, int target, bool atLeast)
    {
        // 누적값만 본다: 판·누적 양쪽에 기록되는 카운터(god_build_* 등)도 이번 판 값이 끼어들지 않게
        if (atLeast) return () => (Profile.Has(name) ? Profile.CountOf(name) : Counters.Get(name)) >= target;
        return () => (Profile.Has(name) || Counters.Has(name)) && (Profile.Has(name) ? Profile.CountOf(name) : Counters.Get(name)) <= target;
    }

    static Func<bool> ParseToken(string token, string whole)
    {
        if (IsCounterToken(token)) return ParseCounter(token, whole);
        if (token == "founded") return () => GameSession.CultFounded;
        if (token == "declared") return () => GameSession.Cult.Doctrines.IsMax(DoctrineSkill.Declaration);

        var match = TokenPattern.Match(token);
        if (!match.Success) throw new ArgumentException($"잘못된 업적 조건 '{token}' ({whole})");
        var key = match.Groups["key"].Value;
        var atLeast = match.Groups["op"].Value == ">=";
        var value = match.Groups["value"].Value;

        Func<bool> Compare(Func<int> read, int target) =>
            atLeast ? () => read() >= target : () => read() <= target;
        int Number() => int.TryParse(value, out var n) ? n : throw new ArgumentException($"숫자가 아님 '{token}' ({whole})");

        switch (key)
        {
            case "age": return Compare(() => GameSession.Age.Years, Number());
            case "cultyears": return Compare(() => GameSession.CultFounded ? GameSession.CultTime.Years : -1, Number());
            case "doctrine": return Compare(() => DoctrineLevel.Of(GameSession.Cult[StatId.Doctrine].Value), Number());
            case "gifted": return Compare(() => GameSession.Personal.Talents.Owned.Count, Number());
            case "achv": return Compare(() => Achievements.CountedUnlocked(), Number());
            case "standing":
                if (!Enum.TryParse<Standing>(value, out var tier)) throw new ArgumentException($"알 수 없는 시선 단계 '{token}' ({whole})");
                var target = (int)tier;
                return () => GameSession.CultFounded && (atLeast ? (int)SocialStanding.Of(GameSession.Cult) >= target : (int)SocialStanding.Of(GameSession.Cult) <= target);
        }

        if (key.StartsWith("c:")) { var name = key.Substring(2); return Compare(() => GameSession.CountOf(name), Number()); }
        if (key.StartsWith("profile:")) { var name = key.Substring(8); return CompareProfile(name, Number(), atLeast); }
        if (key.StartsWith("p:"))
        {
            if (!Enum.TryParse<PersonalStatId>(key.Substring(2), out var stat)) throw new ArgumentException($"알 수 없는 개인 스탯 '{token}' ({whole})");
            return Compare(() => GameSession.Personal[stat].Value, Number());
        }
        if (key.StartsWith("s:"))
        {
            if (!Enum.TryParse<StatId>(key.Substring(2), out var stat)) throw new ArgumentException($"알 수 없는 교단 스탯 '{token}' ({whole})");
            var n = Number();
            return () => GameSession.CultFounded && (atLeast ? GameSession.Cult[stat].Value >= n : GameSession.Cult[stat].Value <= n);
        }
        throw new ArgumentException($"알 수 없는 업적 조건 '{token}' ({whole})");
    }
}
