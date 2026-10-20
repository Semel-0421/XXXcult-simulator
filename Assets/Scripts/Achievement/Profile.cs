using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// 여러 판에 걸쳐 남는 플레이어 기록: 달성한 업적과 누적 카운터. persistentDataPath/profile.json에 저장한다.
public static class Profile
{
    [Serializable]
    class Data
    {
        public List<string> unlocked = new();
        public List<StatEntry> counters = new();
        public bool rewardsEnabled = true;
    }

    // 게임이 실제로 실행 중일 때만 켠다 (에디터 테스트가 기록을 건드리지 않도록)
    public static bool Enabled { get; set; }

    // 시뮬레이션 전용: 켜면 파일을 읽지도 쓰지도 않고 메모리에만 둔다 (Counters도 같이). 기본 꺼짐
    public static bool InMemory { get; set; }

    static string path;
    static HashSet<string> unlocked;
    static Dictionary<string, int> counters;
    static bool rewardsEnabled = true;

    // 꺼 두면 업적은 그대로 달성되지만 기프티드·스탯 보상은 지급되지 않는다
    public static bool RewardsEnabled
    {
        get { Load(); return rewardsEnabled; }
        set { Load(); if (rewardsEnabled == value) return; rewardsEnabled = value; Save(); }
    }

    public static string Path
    {
        get => path ??= System.IO.Path.Combine(Application.persistentDataPath, "profile.json");
        set { path = value; Reset(); }
    }

    public static int UnlockedCount { get { Load(); return unlocked.Count; } }
    public static IReadOnlyCollection<string> Unlocked { get { Load(); return unlocked; } }

    public static bool IsUnlocked(string id) { Load(); return unlocked.Contains(id); }

    public static bool Has(string key) { Load(); return counters.ContainsKey(key); }

    public static int CountOf(string key) { Load(); return counters.TryGetValue(key, out var n) ? n : 0; }

    public static void MarkUnlocked(string id)
    {
        Load();
        if (unlocked.Add(id)) Save();
    }

    public static void Count(string key, int amount = 1)
    {
        if (!Enabled) return;
        Load();
        counters[key] = CountOf(key) + amount;
        Save();
    }

    // 메모리에 올린 기록을 버려 다음 접근 때 파일에서 다시 읽게 한다
    public static void Reset()
    {
        unlocked = null;
        counters = null;
        rewardsEnabled = true;
        Counters.ClearAll();
    }

    static void Load()
    {
        if (unlocked != null) return;
        unlocked = new HashSet<string>();
        counters = new Dictionary<string, int>();
        var data = InMemory ? null : SafeFile.Read(Path, text => JsonUtility.FromJson<Data>(text)); // 깨졌으면 .bak, 그것도 없으면 빈 상태
        if (data == null) return;
        foreach (var id in data.unlocked ?? new List<string>()) unlocked.Add(id);
        foreach (var entry in data.counters ?? new List<StatEntry>()) counters[entry.key] = entry.value;
        rewardsEnabled = data.rewardsEnabled;
    }

    static void Save()
    {
        if (!Enabled || InMemory) return;
        var data = new Data { rewardsEnabled = rewardsEnabled };
        data.unlocked.AddRange(unlocked.OrderBy(id => id, StringComparer.Ordinal));
        foreach (var pair in counters) data.counters.Add(new StatEntry(pair.Key, pair.Value));
        try
        {
            SafeFile.Write(Path, JsonUtility.ToJson(data));
        }
        catch (Exception) { /* 저장 실패는 게임 진행을 막지 않는다 */ }
    }
}
