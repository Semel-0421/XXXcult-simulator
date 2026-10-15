using System.Collections.Generic;
using System.Linq;

// 기프티드 툴팁에 보여줄 글. 히든 스탯(접신·기적)에 대한 효과는 플레이어에게 보여주지 않는다.
public static class GiftedInfo
{
    public static bool IsHidden(PersonalStatId stat) => stat == PersonalStatId.Miracle;
    public static bool IsHidden(StatId stat) => CultStatus.IsHidden(stat);

    public static string CategoryName(Talent talent) => Lang.Get($"gcat_{talent.Category}");

    // 효과 한 줄씩 (보이는 효과가 없으면 빈 목록)
    public static List<string> EffectLines(Talent talent)
    {
        var lines = new List<string>();

        var percents = talent.Modifiers
            .Where(m => m.PersonalStat is { } p ? !IsHidden(p) : m.CultStat is { } c && !IsHidden(c))
            .Select(m => $"{StatName(m)} {Signed(m.Percent)}%");
        var joined = string.Join(", ", percents);
        if (joined.Length > 0) lines.Add(joined);

        foreach (var special in talent.Specials)
        {
            var line = SpecialLine(special);
            if (line != null) lines.Add(line);
        }

        var extra = WithoutHidden(talent.ExtraEffect);
        if (extra.Length > 0) lines.Add(extra);
        return lines;
    }

    static readonly string[] HiddenWords = { "접신", "기적", "Communion", "Miracle" };

    // 기타 효과 설명문에서 히든 스탯을 언급한 문장은 뺀다
    static string WithoutHidden(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var kept = new List<string>();
        foreach (var sentence in text.Split(new[] { ". ", "\n" }, System.StringSplitOptions.RemoveEmptyEntries))
        {
            var s = sentence.Trim();
            if (s.Length == 0 || HiddenWords.Any(word => s.Contains(word))) continue;
            kept.Add(s.EndsWith(".") ? s : s + ".");
        }
        return string.Join(" ", kept);
    }

    static string SpecialLine(GiftedSpecial special) => special.Kind switch
    {
        GiftedSpecial.Type.Chance => string.Format(Lang.Get("special_chance"), ActionName(special.Action), Signed(special.Amount)),
        GiftedSpecial.Type.FailHalf => string.Format(Lang.Get("special_failhalf"), ActionName(special.Action)),
        _ => YearlyLine(special),
    };

    static string YearlyLine(GiftedSpecial special)
    {
        if (special.PersonalStat is { } p)
            return IsHidden(p) ? null : string.Format(Lang.Get("special_year"), Lang.Get($"pstat_{p}"), Signed(special.Amount));
        if (special.CultStat is { } c)
            return IsHidden(c) ? null : string.Format(Lang.Get("special_year"), Lang.Get($"stat_{c}"), Signed(special.Amount));
        return null;
    }

    static string StatName(StatModifier m) =>
        m.PersonalStat is { } p ? Lang.Get($"pstat_{p}") : Lang.Get($"stat_{m.CultStat}");

    static string ActionName(string action) => action switch
    {
        GiftedSpecial.AllActions => Lang.Get("action_all"),
        "Exercise" or "Meditate" or "Study" or "Socialize" => Lang.Get($"life_{action}"),
        "Pray" => Lang.Get("life_Pray"),
        "CultPray" => Lang.Get("action_Pray"),
        _ => Lang.Get($"action_{action}"),
    };

    static string Signed(float value) => (value >= 0 ? "+" : "") + value.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture);
}
