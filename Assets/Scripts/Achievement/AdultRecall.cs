using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 본 성인 이벤트 id를 여러 판에 걸쳐 기억한다 (회상 갤러리용). Profile과 같은 방식(SafeFile)으로 adult_recall.json에 따로 저장한다.
public static class AdultRecall
{
    [Serializable]
    class Data { public List<string> seen = new(); }

    static string path;
    static HashSet<string> seen;

    public static string Path
    {
        get => path ??= System.IO.Path.Combine(Application.persistentDataPath, "adult_recall.json");
        set { path = value; seen = null; }
    }

    public static int SeenCount { get { Load(); return seen.Count; } }

    public static bool IsSeen(string id) { Load(); return seen.Contains(id); }

    // 성인 이벤트가 화면에 뜰 때 부른다. 목록에 없는 id는 무시
    public static void Mark(string eventId)
    {
        if (string.IsNullOrEmpty(eventId) || !AdultTitles.Has(eventId)) return;
        Load();
        if (seen.Add(eventId)) Save();
    }

    static void Load()
    {
        if (seen != null) return;
        seen = new HashSet<string>();
        var data = SafeFile.Read(Path, text => JsonUtility.FromJson<Data>(text));
        if (data?.seen != null) foreach (var id in data.seen) seen.Add(id);
    }

    static void Save()
    {
        if (!Profile.Enabled) return; // 에디터 테스트가 기록을 건드리지 않게 Profile과 같은 조건
        var data = new Data();
        data.seen.AddRange(seen.OrderBy(id => id, StringComparer.Ordinal));
        try { SafeFile.Write(Path, JsonUtility.ToJson(data)); }
        catch (Exception) { /* 저장 실패는 게임 진행을 막지 않는다 */ }
    }
}
