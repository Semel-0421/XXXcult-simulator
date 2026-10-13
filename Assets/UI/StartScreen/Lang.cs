using System;
using System.Collections.Generic;
using UnityEngine;

public static class Lang
{
    public static event Action Changed;
    public static bool IsKorean { get; private set; } = PlayerPrefs.GetString("lang", "en") == "ko";

    static readonly Dictionary<string, (string en, string ko)> Table = new()
    {
        ["new_game"] = ("New Game", "새 게임"), ["continue"] = ("Continue", "이어하기"), ["achievements"] = ("Achievements", "업적"),
        ["settings"] = ("Settings", "설정"), ["quit"] = ("Quit", "종료"), ["language"] = ("Language", "언어"), ["back"] = ("Back", "뒤로"),
        ["stat_LeaderFaith"] = ("Faith", "믿음"), ["stat_FollowerFaith"] = ("Followers' Faith", "신도의 믿음"), ["stat_Authority"] = ("Authority", "권위"), ["stat_Offerings"] = ("Offerings", "봉헌금"),
        ["stat_Followers"] = ("Followers", "신도"), ["stat_Doctrine"] = ("Doctrine", "교리"), ["stat_Communion"] = ("Communion", "접신"), ["stat_Reputation"] = ("Reputation", "명망"),
        ["resume"] = ("Resume", "게임으로 돌아가기"), ["achievements"] = ("Achievements", "업적"), ["exit"] = ("Exit", "나가기"),
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
