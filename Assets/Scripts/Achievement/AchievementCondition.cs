using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// 업적 조건 문자열을 해석해 현재 판 상태로 달성 여부를 판단한다. 공백으로 나눈 조건은 모두 만족해야 한다.
//   founded / declared                 교단 창설 / 교리 선포를 익힘
//   age>=N  cultyears>=N               나이 / 교단 창설 후 햇수
//   doctrine>=N  gifted>=N  achv>=N    교리 레벨 / 보유 기프티드 수 / 달성 업적 수
//   c:KEY>=N                           이번 판 카운터 (events, choice_ok, act_Preach ...)
//   p:Stat>=N  s:Stat>=N               개인 스탯 / 교단 스탯 값 (s는 교단 창설 후에만)
//   standing>=Name  standing<=Name     사회적 시선 단계 (교단 창설 후에만)
//   profile:KEY>=N                     여러 판에 걸친 카운터 (runs, dissolved, founded)
public class AchievementCondition
{
    static readonly Regex TokenPattern = new(@"^(?<key>[A-Za-z_:]+)(?<op>>=|<=)(?<value>[A-Za-z0-9_]+)$", RegexOptions.Compiled);

    readonly List<Func<bool>> checks = new();

    public static AchievementCondition Parse(string text)
    {
        var condition = new AchievementCondition();
        foreach (var token in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            condition.checks.Add(ParseToken(token, text));
        if (condition.checks.Count == 0) throw new ArgumentException("빈 업적 조건");
        return condition;
    }

    public bool IsMet()
    {
        foreach (var check in checks)
            if (!check()) return false;
        return true;
    }

    static Func<bool> ParseToken(string token, string whole)
    {
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
            case "achv": return Compare(() => Profile.UnlockedCount, Number());
            case "standing":
                if (!Enum.TryParse<Standing>(value, out var tier)) throw new ArgumentException($"알 수 없는 시선 단계 '{token}' ({whole})");
                var target = (int)tier;
                return () => GameSession.CultFounded && (atLeast ? (int)SocialStanding.Of(GameSession.Cult) >= target : (int)SocialStanding.Of(GameSession.Cult) <= target);
        }

        if (key.StartsWith("c:")) { var name = key.Substring(2); return Compare(() => GameSession.CountOf(name), Number()); }
        if (key.StartsWith("profile:")) { var name = key.Substring(8); return Compare(() => Profile.CountOf(name), Number()); }
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
