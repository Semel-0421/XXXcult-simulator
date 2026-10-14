using System;
using System.Collections.Generic;

// 저장 파일 한 칸의 내용. JsonUtility로 직렬화하므로 딕셔너리 대신 리스트를 쓴다.
[Serializable]
public class SaveData
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public string savedAt;

    public int seed;
    public string rngState; // ulong을 문자열로 보관

    public string playerName;
    public int gender;
    public int ageTurns;
    public int cultTurns;
    public bool founded;
    public string cultName;

    public List<StatEntry> personal = new();
    public List<StatEntry> cult = new();
    public List<StatEntry> doctrines = new();
    public List<RemainderEntry> personalRemainders = new();
    public List<RemainderEntry> cultRemainders = new();
    public List<string> talents = new();
    public List<string> usedEvents = new();
}

[Serializable]
public struct StatEntry
{
    public string key;
    public int value;

    public StatEntry(string key, int value) { this.key = key; this.value = value; }
}

[Serializable]
public struct RemainderEntry
{
    public string key;
    public float amount;

    public RemainderEntry(string key, float amount) { this.key = key; this.amount = amount; }
}
