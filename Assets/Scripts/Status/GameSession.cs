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
    public static int CultGeneration { get; private set; } // 새 판마다 증가. 세이브/로드 시 Run 카운터 보존용
    public static bool Resuming { get; private set; } // 불러온 직후: 씬이 인트로를 건너뛰고 이어서 진행한다
    public static bool Started { get; private set; }   // 인트로가 끝나 본 진행이 시작됨 (저장 가능)
    public static bool Ended { get; private set; }     // 엔딩을 봄 (더 저장할 수 없음)
    public static string PendingEventId { get; private set; }   // 지금 화면에 떠 있는 이벤트 id (없으면 null)
    public static bool PendingAdvancesTurn { get; private set; } // 그 이벤트에 답하면 1턴이 지나는가

    public static bool Dead { get; private set; }   // 주인공이 세상을 떠남 (연령별 사망률, Mortality)
    public static int Run { get; private set; }     // 이번 판의 회차 (Progress.BeginRun이 정함). 0이면 회차 제한 없음

    public static void SetRun(int run) => Run = run;

    public static bool GodMode { get; private set; }     // 준비된 신 모드로 진행 중인 판
    public static GodState God { get; private set; }     // 그 모드의 권능 트리·포인트

    public static void SetGodMode(bool on) => GodMode = on;

    public static bool MirrorMode { get; private set; }  // 거울 세계 모드로 진행 중인 판 (전용 이벤트·교리)
    public static void SetMirrorMode(bool on) { MirrorMode = on; if (on) MirrorCounters.RunStart(); }

    public static bool EyeMode { get; private set; }     // 황금의 눈동자 모드로 진행 중인 판 (Docs/GDD/systems/SYS_DEMIURGE.md)
    public static void SetEyeMode(bool on) => EyeMode = on;
    public static EyeSector EyeSector { get; private set; } // 첫 질문에서 고른 섹터 = 선택된 데미우르고스
    public static void SetEyeSector(EyeSector sector) => EyeSector = sector;

    // 이번 판이 도달한 엔딩 (음악이 결말 곡으로 바뀐다). 새 판에서 지운다
    public static Endings.Ending Ending { get; private set; }
    public static void SetEnding(Endings.Ending ending) { Ending = ending; MirrorCounters.Ending(ending); }

    // 신의 본성을 고르면 교리 선포를 대신해 교단 행동이 열린다
    static void OnGodSkillLearned(GodSkill skill)
    {
        if (GodMode && God.Tree.IsNature(skill) && !Cult.Doctrines.IsMax(DoctrineSkill.Declaration))
            Cult.Doctrines.LevelUp(DoctrineSkill.Declaration);
    }

    public static string FirstExtremeId { get; private set; } // 가장 먼저 0/최대에 이른 교단 스탯의 엔딩 id (없으면 null)

    // 처음 한 번만 기록한다. 25년이 지나도 최종 스킬이 없을 때 이 스탯의 엔딩이 나온다.
    public static void RecordExtreme(string endingId)
    {
        if (FirstExtremeId == null) FirstExtremeId = endingId;
    }

    public static readonly System.Collections.Generic.Dictionary<StatId, int> ZeroSince = new(); // 스탯이 0이 된 교단 턴 (저장하지 않음, 불러오면 다시 센다)
    public static int RaisedMask { get; private set; } // 한 번이라도 0을 넘은 적이 있는 교단 스탯 (StatId 비트). 시작값 0은 바닥이 아니다.

    public static void MarkRaised(StatId id) => RaisedMask |= 1 << (int)id;

    public static int PersonalRaisedMask { get; private set; } // 한 번이라도 0을 넘은 개인 스탯 (PersonalStatId 비트). 휴식 판정용
    public static void MergePersonalRaised(int mask) => PersonalRaisedMask |= mask;

    public static void MarkStarted() => Started = true;
    public static void MarkEnded() => Ended = true;

    public static void SetPending(string eventId, bool advancesTurn)
    {
        PendingEventId = eventId;
        PendingAdvancesTurn = advancesTurn;
        MirrorCounters.Event(eventId);
    }

    public static void ClearPending() => PendingEventId = null;
    public static HashSet<string> UsedEvents { get; private set; } // 이미 나온 이벤트 id (생애·교단 공용)
    static Dictionary<string, int> counters;                       // 이번 판의 진행 카운터 (업적 조건용)

    static GameSession() => StartNew();

    public static int Misfortune { get; private set; }             // 쌓인 불운 겹 수 (부정적 이벤트마다 +1)

    public static event Action Founded;
    public static event Action MisfortuneChanged;

    public static void AddMisfortune()
    {
        if (Misfortune >= global::Misfortune.MaxStacks || GiftedV2Effects.MisfortuneGainBlockedNormal() || !GiftedV2Mirror.AllowsMisfortuneGain()) return;
        Misfortune++;
        MirrorCounters.Misfortune(Misfortune);
        MisfortuneChanged?.Invoke();
    }

    // 기프티드 효과 전용: 불운은 원래 줄지 않는다 (v2m095, v2m097)
    public static void ReduceMisfortuneByGifted(int amount)
    {
        var next = Math.Max(0, Misfortune - Math.Max(0, amount));
        if (next == Misfortune) return;
        Misfortune = next;
        MisfortuneChanged?.Invoke();
    }

    public static void FoundCult(string name)
    {
        Cult.Name = name;
        CultFounded = true;
        Profile.Count("founded");
        NormalCounters.OnFounded();
        Founded?.Invoke();
        Achievements.Evaluate();
    }

    public static void AdvanceTurn()
    {
        if (Ended) return; // 엔딩 후 턴 진행 차단

        Age.Advance();
        if (CultFounded) CultTime.Advance();
        if (!Dead && !GodMode && Mortality.Dies(Age.Years, Personal.Gender, Rng) && !GiftedV2Effects.TryPreventDeath()) Dead = true; // 분기마다 연령별 사망률 (신의 그릇은 늙지 않는다)

        if (Dead)
        {
            // 사망 시 최소 연산만
            NormalCounters.Refresh();
            Achievements.Evaluate();
            return;
        }

        if (Age.Quarter == 0) GiftedSpecials.ApplyYearly(Personal, Cult, CultFounded); // 해가 바뀔 때
        if (CultFounded && CultTime.Quarter == 0) SocialStanding.YearEnd(Cult); // 교단 해가 바뀔 때 입교
        if (CultFounded) MirrorCounters.Turn();
        if (CultFounded) GiftedV2Mirror.AfterTurn();
        if (GodMode && CultFounded) God.Quarter(Cult);
        if (Age.Quarter == 0 && GodMode && CultFounded) God.YearEnd(Cult, Personal);
        if (Age.Quarter == 0) GiftedV2Effects.OnYearEnd();
        NormalCounters.Refresh();
        HiddenCounters.Turn();
        Achievements.Evaluate();
    }

    public static void Count(string key, int amount = 1) => counters[key] = CountOf(key) + amount;

    public static int CountOf(string key) => counters.TryGetValue(key, out var n) ? n : 0;

    public static void StartNew(int? seed = null)
    {
        Counters.ResetRun();
        Cult = new CultStatus();
        Cult.Doctrines.Changed += MirrorCounters.Doctrine;
        Cult.Doctrines.Changed += NormalCounters.OnDoctrine; // 화면(CultFlow)이 없어도 세도록 상태 쪽에 건다
        Cult.Doctrines.Changed += (skill) => Personal.Talents.MarkPercentCacheDirty(); // 교리 변경 시 기프티드 퍼센트 캐시 무효화
        Personal = new PersonalStatus();
        Personal.Talents.ExtraEffects = () => Cult.Doctrines.EffectTalents; // 배운 교리도 기프티드처럼 효과를 준다
        Seed = seed ?? Environment.TickCount;
        Rng = new GameRng(Seed);
        Age = new GameClock();
        CultTime = new GameClock();
        CultFounded = false;
        CultGeneration++;
        UsedEvents = new HashSet<string>();
        counters = new Dictionary<string, int>();
        NormalCounters.Reset();
        Misfortune = 0;
        Resuming = false;
        Started = false;
        Ended = false;
        Ending = null;
        Dead = false;
        Run = 0;
        GodMode = false;
        MirrorMode = false;
        EyeMode = false;
        EyeSector = EyeSector.None;
        God = new GodState();
        God.Tree.Changed += OnGodSkillLearned;
        FirstExtremeId = null;
        RaisedMask = 0;
        ZeroSince.Clear();
        PersonalRaisedMask = 0;
        PendingEventId = null;
        PendingAdvancesTurn = false;
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
            misfortune = Misfortune,
            doctrineEmblem = Cult.Doctrines.EmblemId,
            firstExtreme = FirstExtremeId,
            statsRaised = RaisedMask,
            personalRaised = PersonalRaisedMask,
            pendingEventId = PendingEventId,
            pendingAdvances = PendingAdvancesTurn,
            run = Run,
            cultGeneration = CultGeneration,
            dead = Dead,
            godMode = GodMode,
            mirrorMode = MirrorMode,
            eyeMode = EyeMode,
            eyeSector = (int)EyeSector,
            godNature = God.Tree.NatureId,
            godPointsEarned = God.Points.Earned,
            godSecularStage = God.Points.SecularStageAwarded,
            godEnvy = God.Pressure.Envy,
            godSuspicion = God.Pressure.Suspicion,
            godDissolution = God.Buildings.DissolutionOrdered,
        };
        data.godBuildings.AddRange(God.Buildings.Levels);
        data.godSkills.AddRange(God.Tree.Learned);
        data.godCores.AddRange(God.Tree.CoreOrder);
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
        foreach (var pair in counters) data.counters.Add(new StatEntry(pair.Key, pair.Value));
        return data;
    }

    // 저장 데이터로 상태를 되살린다. 씬을 다시 불러오면 그 상태에서 이어진다.
    public static void Restore(SaveData data)
    {
        StartNew(data.seed);
        CultGeneration = data.cultGeneration; // 세대 ID 복원 (Run 카운터 보존)
        MirrorMode = data.mirrorMode; // 교리 목록을 고르므로 먼저
        EyeMode = data.eyeMode;
        EyeSector = (EyeSector)data.eyeSector;
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
        Cult.Doctrines.RestoreEmblem(data.doctrineEmblem);
        Personal.Talents.MarkPercentCacheDirty(); // 복원된 교리·기프티드에 맞춰 퍼센트 캐시 재계산
        foreach (var e in data.personalRemainders)
            if (Enum.TryParse<PersonalStatId>(e.key, out var id)) Personal.Remainders.Set(id, e.amount);
        foreach (var e in data.cultRemainders)
            if (Enum.TryParse<StatId>(e.key, out var id)) Cult.Remainders.Set(id, e.amount);
        HiddenCounters.Muted = true;
        try { foreach (var id in data.talents) if (GiftedCatalog.Find(id) != null) GiftedGranter.Grant(Personal.Talents, id); }
        finally { HiddenCounters.Muted = false; }
        foreach (var id in data.usedEvents) UsedEvents.Add(id);
        foreach (var e in data.counters) counters[e.key] = e.value;
        Misfortune = Math.Clamp(data.misfortune, 0, global::Misfortune.MaxStacks);
        Resuming = true;
        Started = true;
        FirstExtremeId = string.IsNullOrEmpty(data.firstExtreme) ? null : data.firstExtreme;
        RaisedMask = data.statsRaised;
        ZeroSince.Clear();
        PersonalRaisedMask = data.personalRaised;
        PendingEventId = string.IsNullOrEmpty(data.pendingEventId) ? null : data.pendingEventId;
        PendingAdvancesTurn = data.pendingAdvances;
        Dead = data.dead;
        GodMode = data.godMode;
        God.Tree.Restore(data.godSkills, data.godNature, data.godCores);
        God.Points.Restore(data.godPointsEarned, data.godSecularStage);
        God.Pressure.Restore(data.godEnvy, data.godSuspicion);
        God.Buildings.Restore(data.godBuildings, data.godDissolution);
        Progress.ApplyRun(data.run); // 회차가 없던 옛 저장(0)은 제한 없이 이어 간다
    }

    // 인트로가 한 번만 건너뛰도록 플래그를 소비한다
    public static bool ConsumeResume()
    {
        var resuming = Resuming;
        Resuming = false;
        return resuming;
    }
}
