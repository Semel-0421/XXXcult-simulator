using System;
using System.Collections.Generic;

// 한 판의 진행 상태 보관소. 씬이 바뀌어도 유지된다.
public static class GameSession
{
    public static CultStatus Cult { get; private set; }
    public static PersonalStatus Personal { get; private set; }
    public static int Seed { get; private set; }
    public static Random Rng { get; private set; }
    public static GameClock Age { get; private set; }      // 플레이어 나이 (시작부터)
    public static GameClock CultTime { get; private set; } // 교단 창설 이후 시간
    public static bool CultFounded { get; private set; }
    public static bool Resuming { get; private set; } // 불러온 직후: 씬이 인트로를 건너뛰고 이어서 진행한다
    public static HashSet<string> UsedEvents { get; private set; } // 이미 나온 이벤트 id (생애·교단 공용)

    static GameSession() => StartNew();

    public static event Action Founded;

    public static void FoundCult(string name)
    {
        Cult.Name = name;
        CultFounded = true;
        Founded?.Invoke();
    }

    public static void AdvanceTurn()
    {
        Age.Advance();
        if (CultFounded) CultTime.Advance();
        if (Age.Quarter == 0) GiftedSpecials.ApplyYearly(Personal, Cult, CultFounded); // 해가 바뀔 때
    }

    public static void StartNew(int? seed = null)
    {
        Cult = new CultStatus();
        Personal = new PersonalStatus();
        Seed = seed ?? Environment.TickCount;
        Rng = new GameRng(Seed);
        Age = new GameClock();
        CultTime = new GameClock();
        CultFounded = false;
        UsedEvents = new HashSet<string>();
        Resuming = false;
    }

    // 행동 선택 시점의 진행 상태를 저장 데이터로 만든다
    public static SaveData Capture()
    {
        var data = new SaveData
        {
            savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            seed = Seed,
            rngState = ((GameRng)Rng).State.ToString(),
            playerName = Personal.Name,
            gender = (int)Personal.Gender,
            ageTurns = Age.Turns,
            cultTurns = CultTime.Turns,
            founded = CultFounded,
            cultName = Cult.Name,
        };
        foreach (PersonalStatId id in Enum.GetValues(typeof(PersonalStatId)))
            data.personal.Add(new StatEntry(id.ToString(), Personal[id].Value));
        foreach (StatId id in Enum.GetValues(typeof(StatId)))
            data.cult.Add(new StatEntry(id.ToString(), Cult[id].Value));
        foreach (var level in Cult.Doctrines.Levels)
            data.doctrines.Add(new StatEntry(level.Key, level.Value));
        foreach (var entry in Personal.Remainders.Entries)
            data.personalRemainders.Add(new RemainderEntry(entry.Key.ToString(), entry.Value));
        foreach (var entry in Cult.Remainders.Entries)
            data.cultRemainders.Add(new RemainderEntry(entry.Key.ToString(), entry.Value));
        foreach (var talent in Personal.Talents.Owned) data.talents.Add(talent.Id);
        data.usedEvents.AddRange(UsedEvents);
        return data;
    }

    // 저장 데이터로 상태를 되살린다. 씬을 다시 불러오면 그 상태에서 이어진다.
    public static void Restore(SaveData data)
    {
        StartNew(data.seed);
        Rng = new GameRng(ulong.Parse(data.rngState));
        Personal.Name = data.playerName ?? "";
        Personal.Gender = (Gender)data.gender;
        Age.Restore(data.ageTurns);
        CultTime.Restore(data.cultTurns);
        CultFounded = data.founded;
        Cult.Name = data.cultName ?? "";

        foreach (var e in data.personal)
            if (Enum.TryParse<PersonalStatId>(e.key, out var id)) Personal[id].Set(e.value);
        foreach (var e in data.cult)
            if (Enum.TryParse<StatId>(e.key, out var id)) Cult[id].Set(e.value);
        foreach (var e in data.doctrines) Cult.Doctrines.Restore(e.key, e.value);
        foreach (var e in data.personalRemainders)
            if (Enum.TryParse<PersonalStatId>(e.key, out var id)) Personal.Remainders.Set(id, e.amount);
        foreach (var e in data.cultRemainders)
            if (Enum.TryParse<StatId>(e.key, out var id)) Cult.Remainders.Set(id, e.amount);
        foreach (var id in data.talents)
            if (GiftedCatalog.Find(id) != null) GiftedGranter.Grant(Personal.Talents, id);
        foreach (var id in data.usedEvents) UsedEvents.Add(id);
        Resuming = true;
    }

    // 인트로가 한 번만 건너뛰도록 플래그를 소비한다
    public static bool ConsumeResume()
    {
        var resuming = Resuming;
        Resuming = false;
        return resuming;
    }
}
