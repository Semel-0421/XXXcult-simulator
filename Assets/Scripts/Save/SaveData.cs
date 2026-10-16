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
    public int misfortune;
    public string doctrineEmblem;
    public int personalRaised;     // 한 번이라도 0을 넘은 개인 스탯 비트마스크 (휴식 판정용)
    public int statsRaised;        // 한 번이라도 0을 넘은 교단 스탯 비트마스크
    public string firstExtreme;    // 가장 먼저 극단(0/최대)에 이른 교단 스탯의 엔딩 id
    public string pendingEventId;  // 저장 시점에 화면에 떠 있던 이벤트 (없으면 비어 있음)
    public bool pendingAdvances;
    public int run;                // 이 판의 회차 (0이면 회차 제한 없음: 회차 기능 이전의 저장)
    public bool dead;              // 주인공이 세상을 떠났음 (엔딩 직전에 저장된 경우)

    // 준비된 신 모드
    public bool godMode;
    public List<string> godSkills = new();   // 익힌 권능 노드
    public string godNature;                 // 신의 본성 (무료로 고른 바깥 노드)
    public List<string> godCores = new();    // 완성한 핵 순서 (승천 엔딩 판정)
    public int godPointsEarned;
    public int godSecularStage;              // 포인트를 이미 받은 세속 규모 단계
    public float godEnvy, godSuspicion;      // 압박 가산치 (신계 시기, 지상 의혹)
    public List<int> godBuildings = new();   // 축성한 건물 레벨 (GodBuildingType 순서)
    public bool godDissolution;              // 해산 명령이 내려짐 (엔딩 직전에 저장된 경우)

    public List<StatEntry> personal = new();
    public List<StatEntry> cult = new();
    public List<StatEntry> doctrines = new();
    public List<RemainderEntry> personalRemainders = new();
    public List<RemainderEntry> cultRemainders = new();
    public List<string> talents = new();
    public List<string> usedEvents = new();
    public List<StatEntry> counters = new();
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
