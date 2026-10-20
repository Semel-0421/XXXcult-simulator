using System;
using System.Collections.Generic;

// 신규 타고난 재능(v2) 효과 프레임워크. 게임 쪽은 아래 훅만 부르고, 개별 효과는 GiftedV2Table에서 Register로 등록한다.
// 보유: 연결 업적(LinkedAchievement)을 프로필에서 달성했으면 보유(회차를 넘어 유지, Achievements.OwnsGiftedV2).
// 적용: Shared면 모든 모드, 아니면 자기 Mode에서만. AdultGated는 성인 업적 옵션이 켜져 있을 때만.
// 아무것도 보유하지 않으면 모든 훅은 무변화(배율 1, 보너스 0, false)이고 난수도 쓰지 않는다.
// 설계 문서: Docs/GDD/systems/SYS_GIFTED_V2_EFFECTS.md
public enum CheckKind { Life, Cult, Event }
public enum ChangeSource { Other, Action, Event }

// 판정 하나. Key: 행동 키(LifeAction 이름, CultActions.Key — 신 모드는 GodRules.ActionOf로 권능 행동 확인) 또는 이벤트 id(모르면 null)
public readonly struct CheckContext
{
    public readonly CheckKind Kind;
    public readonly string Key;
    public readonly float Chance;  // 이 판정의 성공 확률 (모르면 -1) — 재굴림 등에 씀
    public readonly Choice Choice; // 이벤트 판정에서 고른 선택지 (아니면 null)
    public CheckContext(CheckKind kind, string key, float chance = -1f, Choice choice = null) { Kind = kind; Key = key; Chance = chance; Choice = choice; }
    public CheckContext WithChance(float chance) => new(Kind, Key, chance, Choice);
}

// 스탯 변화 하나 (GiftedEffects.Apply를 지나는 것). Cult/Personal 중 하나만 값이 있다. Source/Key는 GiftedV2Effects.During 범위에서 온다.
public readonly struct StatChange
{
    public readonly ChangeSource Source;
    public readonly string Key;
    public readonly StatId? Cult;
    public readonly PersonalStatId? Personal;
    public readonly int Amount;
    public bool IsGain => Amount > 0;
    public StatChange(ChangeSource source, string key, StatId? cult, PersonalStatId? personal, int amount)
    {
        Source = source; Key = key; Cult = cult; Personal = personal; Amount = amount;
    }
}

// 효과 하나의 정의. 쓰지 않는 칸은 null로 둔다. 상태(연속 실패 등)는 Counters run 범위나 GiftedV2Effects 토큰으로 직접 센다.
// CheckWhen/CheckBonusFn/StatWhen/StatMultiplierFn 안에서는 GiftedV2Effects 훅·GiftedEffects.Apply를 부르지 말 것 (공용 버퍼를 도는 중). 나머지 콜백은 불러도 된다.
public sealed partial class EffectDef
{
    // 판정
    public float? CheckBonus;                      // 성공 확률 가산 (0.08 = +8%p)
    public Func<CheckContext, bool> CheckWhen;     // CheckBonus 적용 조건 (null = 모든 판정)
    public Func<CheckContext, float> CheckBonusFn; // 상태 의존 가산 (CheckWhen과 무관하게 항상 불림)
    public Func<CheckContext, bool> RescueFailure; // 실패한 판정을 성공으로 바꾸면 true (토큰·확률은 안에서)
    public Action<CheckContext, bool> OnCheck;     // 최종 결과 통지 (연속·누적 카운트용)

    // 스탯 변화 (증가: ×(1+Σ GainPercent/100), 감소: 크기 ×(1-Σ LossPercent/100), 그다음 StatMultiplierFn을 곱함)
    public float? GainPercent;                     // +15 = 증가 15% 더
    public float? LossPercent;                     // 20 = 감소 20% 덜
    public Func<StatChange, bool> StatWhen;        // Gain/LossPercent 적용 조건 (null = 모든 변화)
    public Func<StatChange, float> StatMultiplierFn;

    // 생사·엔딩
    public Func<bool> PreventDeath;                     // 연령 사망 판정을 막으면 true
    public Func<Endings.Ending, bool> PreventEnding;    // 정해진 엔딩을 막으면 true (막은 뒤 다시 걸리지 않게 상태도 고칠 것)
    public Action<Endings.Ending> OnEnding;             // 엔딩 도달, Endings.Reach 전이라 Endings.Seen(id)==false면 새 엔딩 (다음 판 보너스는 Counters profile 범위에 적어 두고 OnRunStart에서 읽기)

    // 시간
    public Action OnRunStart;                      // 새 판 시작 (일반·거울: 시작 보상 뒤, 신: GodRules.Begin 끝)
    public Action OnYearEnd;                       // 나이 기준 해가 바뀔 때 (GameSession.AdvanceTurn, Age.Quarter == 0)
}

public static partial class GiftedV2Effects
{
    public const string TokenPrefix = "gv2used:";

    static readonly Dictionary<string, EffectDef> defs = new();
    static readonly List<(GiftedV2Catalog.Entry entry, EffectDef def)> registered = new();
    static bool tableLoaded;

    // 테스트에서 성인 옵션을 PlayerPrefs 없이 바꾸기 위한 자리
    public static Func<bool> AdultEnabled = () => AdultGate.AchievementsEnabled;

    public static void Register(string id, EffectDef def)
    {
        var entry = GiftedV2Catalog.Find(id) ?? throw new ArgumentException($"알 수 없는 v2 기프티드 id: {id}");
        if (def == null) throw new ArgumentNullException(nameof(def));
        if (!defs.TryAdd(id, def)) throw new ArgumentException($"v2 기프티드 효과 중복 등록: {id}");
        registered.Add((entry, def));
    }

    public static bool IsRegistered(string id) { EnsureTable(); return defs.ContainsKey(id); }

    static void EnsureTable()
    {
        if (tableLoaded) return;
        tableLoaded = true;
        GiftedV2Table.RegisterAll();
    }

    public static string CurrentMode => GameSession.GodMode ? "god" : GameSession.MirrorMode ? "mirror" : "normal";

    // 공유 규칙 + 성인 게이트 (보유 여부와 무관)
    public static bool AppliesIn(GiftedV2Catalog.Entry e, string mode) =>
        (e.Shared || e.Mode == mode) && (!e.AdultGated || AdultEnabled());

    public static bool IsActive(GiftedV2Catalog.Entry e, string mode = null) =>
        e != null && Achievements.OwnsGiftedV2(e) && AppliesIn(e, mode ?? CurrentMode);

    public static bool IsActive(string id, string mode = null) => IsActive(GiftedV2Catalog.Find(id), mode);

    // 그 모드(null = 지금 판)에서 효과가 나는 보유 v2 기프티드 (등록 여부와 무관, UI·디버그용)
    public static IEnumerable<GiftedV2Catalog.Entry> Owned(string mode = null)
    {
        foreach (var e in GiftedV2Catalog.All)
            if (IsActive(e, mode)) yield return e;
    }

    // shortcut: 매 호출마다 등록된 항목을 훑어 해시 조회함(최대 120개). 느려지면 Profile.UnlockedCount·모드·성인 옵션 기준으로 캐시할 것.
    static readonly List<EffectDef> activeBuffer = new();
    static List<EffectDef> Active()
    {
        EnsureTable();
        activeBuffer.Clear();
        if (registered.Count == 0 || !Achievements.V2OwnershipOn) return activeBuffer;
        var mode = CurrentMode;
        foreach (var (entry, def) in registered)
            if (IsActive(entry, mode)) activeBuffer.Add(def);
        return activeBuffer;
    }

    // ---- 회차당 토큰 (Counters run 범위, 새 판이면 자동으로 비워짐) ----
    // shortcut: Counters run 범위는 세이브에 들어가지 않음 → 이어하기하면 토큰이 다시 찬다. 막으려면 GameSession.Capture/Restore에 넣을 것.
    public static int TokensUsed(string id) => Counters.Get(TokenPrefix + id, perRun: true);

    public static bool TryUseToken(string id, int perRun = 1)
    {
        if (TokensUsed(id) >= perRun) return false;
        Counters.Add(TokenPrefix + id, 1, perRun: true);
        return true;
    }

    // ---- 판정 ----
    public static float SuccessChanceBonus(CheckContext c)
    {
        float bonus = 0;
        foreach (var d in Active())
        {
            if (d.CheckBonus is { } b && (d.CheckWhen == null || d.CheckWhen(c))) bonus += b;
            if (d.CheckBonusFn != null) bonus += d.CheckBonusFn(c);
        }
        return bonus;
    }

    // 굴린 결과를 받아 실패 구제를 시도하고 최종 결과를 통지한 뒤 돌려준다
    public static bool ResolveCheck(CheckContext c, bool success)
    {
        var active = Active();
        if (active.Count == 0) return success;
        var list = active.ToArray(); // 콜백 안에서 다른 훅을 불러도 버퍼가 바뀌지 않게
        if (!success)
            foreach (var d in list)
                if (d.RescueFailure != null && d.RescueFailure(c)) { success = true; break; }
        foreach (var d in list) d.OnCheck?.Invoke(c, success);
        return success;
    }

    // ---- 스탯 변화 ----
    static ChangeSource source;
    static string sourceKey;

    // 이 범위 안의 GiftedEffects.Apply는 출처(행동·이벤트)를 달고 들어온다: using (GiftedV2Effects.During(...)) { ... }
    public static Scope During(ChangeSource s, string key)
    {
        var scope = new Scope(source, sourceKey);
        source = s; sourceKey = key;
        return scope;
    }

    public readonly struct Scope : IDisposable
    {
        readonly ChangeSource prev; readonly string prevKey;
        public Scope(ChangeSource prev, string prevKey) { this.prev = prev; this.prevKey = prevKey; }
        public void Dispose() { source = prev; sourceKey = prevKey; }
    }

    public static float StatMultiplier(PersonalStatId stat, int amount) => StatMultiplier(new StatChange(source, sourceKey, null, stat, amount));
    public static float StatMultiplier(StatId stat, int amount) => StatMultiplier(new StatChange(source, sourceKey, stat, null, amount));

    public static float StatMultiplier(StatChange c)
    {
        if (c.Amount == 0) return 1f;
        var active = Active();
        if (active.Count == 0) return 1f;
        float percent = 0, product = 1f;
        foreach (var d in active)
        {
            if (d.StatWhen == null || d.StatWhen(c))
                percent += c.IsGain ? d.GainPercent ?? 0 : -(d.LossPercent ?? 0);
            if (d.StatMultiplierFn != null) product *= d.StatMultiplierFn(c);
        }
        return Math.Max(0f, 1f + percent / 100f) * product;
    }

    // ---- 생사·엔딩 ----
    public static bool TryPreventDeath()
    {
        foreach (var d in Active().ToArray())
            if (d.PreventDeath != null && d.PreventDeath()) return true;
        return false;
    }

    // 정해진 엔딩을 막을 효과가 있으면 null을 돌려준다 (Progress.ForcedEnding/DueEnding이 거침)
    public static Endings.Ending FilterEnding(Endings.Ending ending)
    {
        if (ending == null) return null;
        foreach (var d in Active().ToArray())
            if (d.PreventEnding != null && d.PreventEnding(ending)) return null;
        return ending;
    }

    public static void OnEnding(Endings.Ending ending) { foreach (var d in Active().ToArray()) d.OnEnding?.Invoke(ending); }

    // ---- 시간 ----
    public static void OnRunStart() { foreach (var d in Active().ToArray()) d.OnRunStart?.Invoke(); }
    public static void OnYearEnd() { foreach (var d in Active().ToArray()) d.OnYearEnd?.Invoke(); }

    // 테스트용: 등록표를 비우고 다음 접근 때 GiftedV2Table을 다시 읽게 한다 (registerTable=false면 표 없이 빈 상태로)
    public static void ResetForTests(bool registerTable = true)
    {
        defs.Clear(); registered.Clear(); tableLoaded = !registerTable;
        source = ChangeSource.Other; sourceKey = null;
    }
}
