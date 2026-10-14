using System;
using System.Collections.Generic;
using UnityEngine;

public static class Lang
{
    public static event Action Changed;
    public static bool IsKorean { get; private set; } = PlayerPrefs.GetString("lang", "ko") == "ko";

    static readonly Dictionary<string, (string en, string ko)> Table = new()
    {
        ["new_game"] = ("New Game", "새 게임"), ["continue"] = ("Continue", "이어하기"), ["achievements"] = ("Achievements", "업적"),
        ["settings"] = ("Settings", "설정"), ["quit"] = ("Quit", "종료"), ["language"] = ("Language", "언어"), ["back"] = ("Back", "뒤로"),
        ["stat_LeaderFaith"] = ("Faith", "믿음"), ["stat_FollowerFaith"] = ("Followers' Faith", "신도의 믿음"), ["stat_Authority"] = ("Authority", "권위"), ["stat_Offerings"] = ("Offerings", "봉헌금"),
        ["stat_Followers"] = ("Followers", "신도"), ["stat_Doctrine"] = ("Doctrine", "교리"), ["stat_Communion"] = ("Communion", "접신"), ["stat_Reputation"] = ("Reputation", "명망"),
        ["resume"] = ("Resume", "게임으로 돌아가기"), ["achievements"] = ("Achievements", "업적"), ["exit"] = ("Exit", "나가기"),
        ["pstat_Health"] = ("Health", "체력"), ["pstat_Mentality"] = ("Mentality", "정신력"), ["pstat_Piety"] = ("Piety", "신앙심"), ["pstat_Repute"] = ("Repute", "평판"), ["pstat_Wealth"] = ("Wealth", "재산"),
        ["pstat_Lust"] = ("Lust", "음욕"), ["pstat_Greed"] = ("Greed", "재물욕"), ["pstat_Appetite"] = ("Appetite", "식욕"), ["pstat_Ambition"] = ("Ambition", "권력욕"),
        ["pstat_Temperance"] = ("Temperance", "절제"), ["pstat_Charity"] = ("Charity", "자애"), ["pstat_Patience"] = ("Patience", "인내"), ["pstat_Integrity"] = ("Integrity", "청렴"),
        ["desire"] = ("Desire", "욕망"), ["virtue"] = ("Virtue", "덕목"), ["name_hint"] = ("Name", "이름"),
        ["confirm"] = ("Confirm", "확인"), ["choice_hint"] = ("Number", "번호 입력"), ["paused"] = ("Paused", "일시정지"),
        ["talents"] = ("Gifted", "기프티드"), ["talent_title"] = ("Owned Gifted", "보유 기프티드"), ["talent_empty"] = ("No Gifted yet.", "보유한 기프티드가 없습니다."), ["close"] = ("Close", "닫기"),
        ["doctrine_level"] = ("Lv.{0}", "{0}레벨"), ["doctrine_max"] = ("MAX", "MAX레벨"),
        ["intro_prompt"] = ("Your first memory was your parents calling your name. What is your name?", "당신의 첫 기억은 부모가 부르는 당신의 이름이었다. 당신의 이름은?"),
    };

    public static string Get(string key) => Table.TryGetValue(key, out var text) ? (IsKorean ? text.ko : text.en) : key;

    public static void Set(bool korean)
    {
        IsKorean = korean;
        PlayerPrefs.SetString("lang", korean ? "ko" : "en");
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
