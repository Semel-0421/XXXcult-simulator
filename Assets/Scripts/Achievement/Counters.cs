using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// 업적 조건용 카운터 저장소. 키 이름은 Docs/achievements_v2_*.csv 의 "NEW:<키>" 와 같다 (예: god_act_Wonder).
//   Profile 범위: 회차를 넘어 쌓이는 누적값 (파일에 저장).
//   Run 범위: 현재 회차 안에서만 세는 값. 새 판(GameSession.StartNew로 Cult가 새로 만들어짐)이 되면 자동으로 비움.
//     shortcut: Run 범위는 메모리에만 있고 세이브 슬롯에 넣지 않음 -> 이어하기로 불러오면 Run 값은 0부터 다시 셈. SaveData에 넣으려면 GameSession.Capture/Restore 수정 필요.
// 게임 쪽 코드는 Add/Max/Set 으로 올리기만 하고, 업적 판정은 Get 으로 읽는다.
public static class Counters
{
    static readonly Dictionary<string, int> profile = new();
    static readonly Dictionary<string, int> run = new();

    static int _lastSeenGeneration; // 마지막으로 본 세대 ID. 바뀔 때만 Run 카운터 초기화
    static bool loaded, dirty;
    static bool _notifyingAchievements; // 재진입 방지 (Counters.Add → Achievements.OnCounterChanged → Unlock → GiftedGranter → Counters.Add)

    public static void Add(string key, int amount = 1, bool perRun = false) { Sync(); Bump(perRun ? run : profile, key, amount); Touch(perRun); Notify(key); }
    public static void Max(string key, int value, bool perRun = false) { Sync(); var d = perRun ? run : profile; if (Get(key, perRun) < value) { d[key] = value; Touch(perRun); Notify(key); } }
    public static void Set(string key, int value, bool perRun = false) { Sync(); (perRun ? run : profile)[key] = value; Touch(perRun); Notify(key); }

    static void Notify(string key)
    {
        if (_notifyingAchievements) return;
        try { _notifyingAchievements = true; Achievements.OnCounterChanged(key); }
        finally { _notifyingAchievements = false; }
    }
    public static int Get(string key, bool perRun = false) { Sync(); return (perRun ? run : profile).TryGetValue(key, out var v) ? v : 0; }

    // 두 범위를 합쳐 읽는다: 조건식에서 범위를 따로 적지 않아도 되게 (Run 값이 있으면 그것, 없으면 Profile)
    public static int Read(string key) { Sync(); return run.TryGetValue(key, out var v) ? v : Get(key); }

    // 한 번이라도 값이 기록된 키인지. "<=, ==" 조건이 아직 세지 않은 카운터 때문에 바로 달성되는 것을 막는다
    public static bool Has(string key) { Sync(); return run.ContainsKey(key) || profile.ContainsKey(key); }

    public static void ResetRun() => run.Clear();

    // 테스트·프로필 초기화용: 메모리 값을 버리고 다음 접근 때 파일에서 다시 읽게 한다
    public static void ClearAll() { profile.Clear(); run.Clear(); loaded = false; dirty = false; _lastSeenGeneration = -1; }

    static void Touch(bool perRun) { if (!perRun) dirty = true; }

    static void Sync()
    {
        if (!loaded && Profile.Enabled) { loaded = true; Load(); }
        var currentGen = GameSession.CultGeneration;
        if (_lastSeenGeneration == currentGen) return;
        _lastSeenGeneration = currentGen;
        run.Clear();
    }

    static string FilePath => System.IO.Path.ChangeExtension(Profile.Path, null) + "_counters.json"; // profile.json 옆 profile_counters.json

    [Serializable] class Data { public List<StatEntry> counters = new(); }

    static void Load()
    {
        if (Profile.InMemory) return;
        var data = SafeFile.Read(FilePath, text => JsonUtility.FromJson<Data>(text));
        if (data?.counters == null) return;
        foreach (var e in data.counters) profile[e.key] = e.value; // 메모리에 이미 쌓인 값은 파일 값으로 덮음 (Enabled 전에 쌓인 값은 없다고 가정)
    }

    // Profile 범위만 저장. Achievements.Evaluate 끝에서 불린다. 게임 실행 중(Profile.Enabled)일 때만 쓴다.
    public static void Save()
    {
        if (!dirty || !Profile.Enabled || Profile.InMemory) return;
        Sync();
        var data = new Data();
        foreach (var pair in profile.OrderBy(p => p.Key, StringComparer.Ordinal)) data.counters.Add(new StatEntry(pair.Key, pair.Value));
        try { SafeFile.Write(FilePath, JsonUtility.ToJson(data)); dirty = false; }
        catch (Exception) { /* 저장 실패는 게임 진행을 막지 않는다 */ }
    }

    static void Bump(Dictionary<string, int> d, string key, int amount) => d[key] = (d.TryGetValue(key, out var v) ? v : 0) + amount;
}
