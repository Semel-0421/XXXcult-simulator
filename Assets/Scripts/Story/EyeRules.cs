using System;
using System.Linq;
using System.Collections.Generic;

// 황금의 눈동자 모드 (Docs/GDD/systems/SYS_DEMIURGE.md). 준비된 신과 거울 세계를 합친 세 번째 특수 모드.
//  - 해금 (DEC-028 A): 준비된 신의 승천 엔딩 하나 + 거울 세계 전용 엔딩 하나를 모두 봐야 열린다.
//    신 모드 패배 엔딩(그릇의 붕괴·잊힌 신·해산된 교단)은 세지 않는다.
//  - 시작 (DEC-027·028 D): 신 모드의 성인 그릇 상태(GodRules.StartVessel) 위에서 돈다. GodMode도 켜서 그릇·접신·압박·축성·전용 이벤트를 그대로 쓰고,
//    다른 점은 EyeMode로 가른다: 기프티드 없음, 신 모드 업적 카운터 안 쌓음, 신의 본성 선택 없이 창설과 동시에 교단 행동이 열림, 패배 엔딩 없음.
//  - 첫 질문(눈동자) → 이름 → 성별 순서로 묻는다 (IntroSequence). 고른 섹터의 0단계 스킬 2개를 바로 얻는다 (AeonTree.Grant).
//  - 아이온 트리(AeonTree)는 GodState.Aeons에 있고, 효과·포인트·단계·건물 해금은 GodTree 계산에 더해진다.
//  - 데미우르고스 스탯·재앙(4단계), 아이온 트리 화면(5단계), 엔딩(6단계)은 아직 없다.
public enum EyeSector { None, Yahweh, Satan, Samael, Yaldabaoth }

public static class EyeRules
{
    public static readonly EyeSector[] Sectors = { EyeSector.Yahweh, EyeSector.Satan, EyeSector.Samael, EyeSector.Yaldabaoth };

    // 새 판: 성인 그릇 + 모드 플래그. 섹터는 첫 질문에서 정한다
    public static void Begin(int? seed = null)
    {
        GodRules.StartVessel(seed);
        GameSession.SetEyeMode(true);
        Profile.Count("eye_runs");
    }

    public static void ChooseSector(EyeSector sector)
    {
        if (sector == EyeSector.None) return;
        GameSession.SetEyeSector(sector);
        EyeCounters.OnSector(sector);
        GameSession.God.Aeons.Grant(sector); // 고른 섹터의 0단계 선·악 두 노드를 처음부터 얻는다 (DEC-027)
        if (!GameSession.God.Demiurge.Copied) GameSession.God.Demiurge.Copy(GameSession.Cult, GameSession.Personal); // 첫 스킬을 얻은 순간 교주의 스탯을 복사
    }

    // ---- 재앙 (SYS_DEMIURGE 5절. 수치는 가안) ----

    public const int CalamityThreshold = 40; // 카운터가 이만큼 차면 재앙
    public const int ZeroPenalty = 10;       // 지키는 스탯이 0이 되면 패배 대신 카운터 +10 (DEC-028 L)
    public const float DamageBase = 0.05f, DamagePerEvil = 0.01f; // 재앙 피해 = 현재 값 × (5% + 악한 아이온 수 × 1%)
    public const int MaxCalamityGuard = 40;  // 선한 아이온의 피해 감소는 40%까지

    static readonly StatId[] ZeroCult = { StatId.Followers, StatId.FollowerFaith, StatId.Reputation };
    static readonly PersonalStatId[] ZeroPersonal = { PersonalStatId.Health, PersonalStatId.Mentality };

    // 섹터마다 재앙이 주로 빼앗는 교단·개인 스탯 (6-1 표의 주력 스탯)
    static (StatId a, StatId b, PersonalStatId p, PersonalStatId q) Targets(EyeSector sector) => sector switch
    {
        EyeSector.Yahweh => (StatId.Doctrine, StatId.FollowerFaith, PersonalStatId.Piety, PersonalStatId.Mentality),
        EyeSector.Satan => (StatId.Offerings, StatId.Authority, PersonalStatId.Wealth, PersonalStatId.Repute),
        EyeSector.Samael => (StatId.Followers, StatId.FollowerFaith, PersonalStatId.Health, PersonalStatId.Mentality),
        _ => (StatId.Reputation, StatId.Authority, PersonalStatId.Repute, PersonalStatId.Health),
    };

    // 섹터별 분기당 카운터: 그 섹터 악한 아이온·그노시스의 재앙 합 - 그 섹터 선한 아이온 수 (최소 0)
    public static int SectorGain(AeonTree tree, EyeSector sector)
    {
        var skills = tree.Learned.Select(AeonSkillCatalog.Find).Where(s => s != null && s.Sector == sector).ToList();
        return Math.Max(0, skills.Sum(s => s.Calamity) - skills.Count(s => s.Kind == AeonKind.Good));
    }

    public static int CounterGain(AeonTree tree) => Sectors.Sum(s => SectorGain(tree, s));

    // 분기마다: 카운터를 쌓고, 지키는 스탯이 0이면 카운터 +10.
    // 교단 스탯은 1로 되살린다. 체력·정신력은 되살리지 않고 본편처럼 휴식으로 회복한다 (휴식 첫 분기에 +3이 되어 벌점은 한 번만)
    public static void Quarter()
    {
        var god = GameSession.God;
        god.Demiurge.AddCounter(CounterGain(god.Aeons));
        foreach (var id in ZeroCult)
            if (GameSession.Cult[id].Value <= 0) { GameSession.Cult[id].Set(1); god.Demiurge.AddCounter(ZeroPenalty); }
        foreach (var id in ZeroPersonal)
            if (GameSession.Personal[id].Value <= 0) god.Demiurge.AddCounter(ZeroPenalty);
        EyeCounters.OnQuarter();
    }

    public static bool CalamityDue => GameSession.EyeMode && GameSession.CultFounded && GameSession.God.Demiurge.Counter >= CalamityThreshold;

    // 재앙을 일으키는 섹터: 카운터를 가장 많이 쌓는 섹터 (같으면 시작 섹터)
    public static EyeSector CalamitySector(AeonTree tree)
    {
        var best = tree.StartSector == EyeSector.None ? EyeSector.Yahweh : tree.StartSector;
        foreach (var sector in Sectors)
            if (SectorGain(tree, sector) > SectorGain(tree, best)) best = sector;
        return best;
    }

    public static float DamageRate(AeonTree tree)
    {
        var evil = tree.Learned.Count(id => AeonSkillCatalog.Find(id)?.Kind == AeonKind.Evil);
        var guard = Math.Min(tree.CalamityGuard, MaxCalamityGuard) / 100f;
        return (DamageBase + evil * DamagePerEvil) * (1 - guard);
    }

    static int Loss(int value, float rate) => value <= 0 ? 0 : Math.Max(1, (int)Math.Round(value * rate, MidpointRounding.AwayFromZero));

    static int[] cultBefore, personalBefore;

    // 재앙 이벤트: 무엇을 내줄지 고른다 (교단 둘 / 몸 둘 / 하나씩). 고르기 전 스탯을 기억해 둔다
    public static Question BeginCalamity()
    {
        var god = GameSession.God;
        var cult = GameSession.Cult;
        var personal = GameSession.Personal;
        cultBefore = DemiurgeState.CultStats.Select(id => cult[id].Value).ToArray();
        personalBefore = DemiurgeState.PersonalStats.Select(id => personal[id].Value).ToArray();

        var sector = CalamitySector(god.Aeons);
        var rate = DamageRate(god.Aeons);
        var (a, b, p, q) = Targets(sector);
        int la = Loss(cult[a].Value, rate), lb = Loss(cult[b].Value, rate), lp = Loss(personal[p].Value, rate), lq = Loss(personal[q].Value, rate);
        string C(StatId id, int n) => $"{StatLabel.Of(id)} -{n:N0}";
        string P(PersonalStatId id, int n) => $"{Lang.Get($"pstat_{id}")} -{n}";
        var done = string.Format(Lang.Get("eye_calamity_done"), Lang.Get(SectorKey(sector)));
        return new Question(string.Format(Lang.Get("eye_calamity"), Lang.Get(SectorKey(sector))),
            new Choice($"{Lang.Get("eye_calamity_cult")} ({C(a, la)}, {C(b, lb)})", new Outcome(done).C(a, -la).C(b, -lb)),
            new Choice($"{Lang.Get("eye_calamity_body")} ({P(p, lp)}, {P(q, lq)})", new Outcome(done).P(p, -lp).P(q, -lq)),
            new Choice($"{Lang.Get("eye_calamity_half")} ({C(a, la)}, {P(p, lp)})", new Outcome(done).C(a, -la).P(p, -lp)));
    }

    // 재앙이 끝난 뒤: 줄어든 만큼 데미우르고스에게 더하고 카운터를 비운다. 가져간 것을 한 줄로 돌려준다
    public static string EndCalamity()
    {
        var demiurge = GameSession.God.Demiurge;
        var taken = new System.Collections.Generic.List<string>();
        for (int i = 0; cultBefore != null && i < DemiurgeState.CultStats.Length; i++)
        {
            var id = DemiurgeState.CultStats[i];
            var lost = cultBefore[i] - GameSession.Cult[id].Value;
            if (lost <= 0) continue;
            demiurge.Absorb(id, lost);
            taken.Add($"{StatLabel.Of(id)} +{lost:N0}");
        }
        for (int i = 0; personalBefore != null && i < DemiurgeState.PersonalStats.Length; i++)
        {
            var id = DemiurgeState.PersonalStats[i];
            var lost = personalBefore[i] - GameSession.Personal[id].Value;
            if (lost <= 0) continue;
            demiurge.Absorb(id, lost);
            taken.Add($"{Lang.Get($"pstat_{id}")} +{lost}");
        }
        var cultLost = taken.Count(t => DemiurgeState.CultStats.Any(id => t.StartsWith(StatLabel.Of(id))));
        EyeCounters.OnCalamity(cultLost == taken.Count ? "cult" : cultLost == 0 ? "body" : "half");
        cultBefore = personalBefore = null;
        demiurge.ResetCounter();
        return taken.Count == 0 ? "" : string.Format(Lang.Get("eye_calamity_taken"), string.Join(", ", taken));
    }

    // 휴식 중 재앙 (2026-10-26 결정): 고를 수 없고, 최대치 대비 가장 높은 스탯 하나에서 피해율의 두 배를 빼앗는다.
    // 교단 스탯(최대 1000)을 키워 둘수록 그것을 내주게 되고, 낮은 스탯으로 휴식에 들어가면 그 위험을 피할 수 없다.
    public const float RestDamageScale = 2f;

    public static string RestCalamity()
    {
        var god = GameSession.God;
        var cult = GameSession.Cult;
        var personal = GameSession.Personal;
        var rate = DamageRate(god.Aeons) * RestDamageScale;
        StatId? bestCult = null; PersonalStatId? bestPersonal = null; float best = -1;
        foreach (var id in DemiurgeState.CultStats)
        {
            var part = id == StatId.Followers ? cult[id].Value / (float)DemiurgeState.FollowerScale : cult[id].Value / (float)cult[id].Max;
            if (part > best) { best = part; bestCult = id; bestPersonal = null; }
        }
        foreach (var id in DemiurgeState.PersonalStats)
        {
            var part = personal[id].Value / (float)PersonalStatus.MaxValue;
            if (part > best) { best = part; bestPersonal = id; bestCult = null; }
        }
        string name; int lost;
        if (bestCult is { } c)
        {
            var before = cult[c].Value;
            cult[c].Add(-Loss(before, rate));
            lost = before - cult[c].Value;
            god.Demiurge.Absorb(c, lost);
            name = StatLabel.Of(c);
        }
        else
        {
            var p = bestPersonal.Value;
            var before = personal[p].Value;
            personal[p].Add(-Loss(before, rate));
            lost = before - personal[p].Value;
            god.Demiurge.Absorb(p, lost);
            name = Lang.Get($"pstat_{p}");
        }
        god.Demiurge.ResetCounter();
        EyeCounters.OnCalamity("rest");
        return string.Format(Lang.Get("eye_rest_calamity"), Lang.Get(SectorKey(CalamitySector(god.Aeons))), name, lost.ToString("N0"));
    }

    // ---- 이벤트 (SYS_DEMIURGE 8절, DEC-028 C) ----
    // 조건 이벤트(그노시스 단계 진입·찬탈 직전·세상이 뒤집힘) → 신 모드 압박 이벤트 → 성인 → 확률 이벤트.
    // 확률 이벤트는 EyeShare 확률로 익힌 섹터의 전용 이벤트, 아니면 신 모드 이벤트(그노시스를 처음 익힌 뒤에는 거울 세계 이벤트도 섞임).
    public const float EyeShare = 0.4f;

    public static bool Reached(EyeSector sector, int level) => GameSession.God.Aeons.IsLearned(AeonSkillCatalog.Stage(sector, level));

    public static bool ReadyToAscend(EyeSector sector) =>
        GameSession.God.Aeons.AscensionSector == sector && Reached(sector, AeonSkill.ThresholdLevel)
        && DoctrineLevel.Of(GameSession.Cult[StatId.Doctrine].Value) >= AeonSkill.AscensionLevel;

    // 처음 그노시스를 익힌 뒤: 세상이 거울처럼 뒤집힌다 (본편 → 거울 세계 평판 기준)
    public static bool Reversed => Sectors.Any(s => Reached(s, AeonSkill.GnosisLevel));

    // 전용 확률 이벤트의 섹터 (id 앞 두 글자)
    public static EyeSector SectorOf(CultEvent e) => e.Id.Length < 2 ? EyeSector.None : e.Id.Substring(0, 2) switch
    {
        "ya" => EyeSector.Yahweh, "sa" => EyeSector.Satan, "sm" => EyeSector.Samael, "yd" => EyeSector.Yaldabaoth, _ => EyeSector.None,
    };

    static bool Touched(EyeSector sector) => sector != EyeSector.None && GameSession.God.Aeons.Learned.Any(id => AeonSkillCatalog.Find(id)?.Sector == sector);

    static CultEvent[] SectorPool() => EyeEvents.All.Where(e => !e.IsConditional && Touched(SectorOf(e))).ToArray();

    static CultEvent[] generalPool, reversedPool;
    static CultEvent[] GeneralPool(bool reversed) => reversed
        ? reversedPool ??= GodEvents.All.Concat(MirrorEvents.All.Where(e => !e.IsConditional && !e.IsNegative)).ToArray()
        : generalPool ??= GodEvents.All;

    // 불러오기에서 저장 시점의 이벤트를 찾는 전체 목록 (Turns.CultPool)
    public static IReadOnlyList<CultEvent> EventPool => eventPool ??= EyeEvents.All.Concat(GodEvents.All).Concat(MirrorEvents.All).ToArray();
    static CultEvent[] eventPool;

    public static CultEvent NextEvent(CultStatus cult, ISet<string> used, System.Random rng, Func<CultEvent> rollAdult)
    {
        var next = CultEvent.FindTriggered(EyeEvents.All, cult, used)
                   ?? CultEvent.RollNegative(GodEvents.All, cult, used, rng, GameSession.God.Pressure.EventChance(cult))
                   ?? rollAdult();
        if (next != null) return next;
        var sector = SectorPool();
        if (sector.Length > 0 && rng.NextDouble() < EyeShare) return CultEvent.RollRandom(sector, cult, used, rng);
        return CultEvent.RollRandom(GeneralPool(Reversed), cult, used, rng);
    }

    // ---- 엔딩 (SYS_DEMIURGE 7절, DEC-027·028 M·O) ----
    // 승천 노드를 익힌 순간 판정한다. 패배 엔딩은 없다.
    //  1) 주도권이 2배를 넘으면 그 섹터의 배드(주도권 상실)
    //  2) 네 섹터 모두 5단계 그노시스를 익혔으면 히든 "네 얼굴을 삼킨 자"
    //  3) 시작 섹터와 다른 섹터로 승천하면 히든 "다른 눈동자"
    //  4) 그 밖에는 그 섹터의 굿(찬탈)
    public static Endings.Ending DueEnding()
    {
        var aeons = GameSession.God.Aeons;
        if (!aeons.Ascended) return null;
        var key = aeons.AscensionSector.ToString().ToLowerInvariant();
        if (Dominance > LoseDominanceAt) return EyeEndings.Find($"eye_{key}_bad");
        if (Sectors.All(s => aeons.IsLearned(AeonSkillCatalog.Stage(s, AeonSkill.GnosisLevel)))) return EyeEndings.Find("eye_four_faces");
        if (aeons.AscensionSector != aeons.StartSector) return EyeEndings.Find($"eye_{key}_other");
        return EyeEndings.Find($"eye_{key}_good");
    }

    public static float Dominance => GameSession.God.Demiurge.Ratio(GameSession.Cult, GameSession.Personal);
    public const float LoseDominanceAt = 2f; // 이 배를 넘으면 주도권을 뺏긴다 (승천 순간 판정: 6단계)

    // 교단 창설: 신의 본성 대신 바로 교리 선포를 채워 교단 행동을 연다
    public static void OnFounded()
    {
        var doctrines = GameSession.Cult.Doctrines;
        if (!doctrines.IsMax(DoctrineSkill.Declaration)) doctrines.LevelUp(DoctrineSkill.Declaration);
    }

    public static string SectorKey(EyeSector sector) => $"eye_sector_{sector.ToString().ToLowerInvariant()}";

    // 첫 질문 글: 질문 + 번호 붙은 답 4개. 답은 <link="e1">~<link="e4">로 눌러 고른다
    public static string QuestionText()
    {
        var lines = Sectors.Select((s, i) => $"{i + 1}. <link=\"e{i + 1}\"><color=#F2D978>{Lang.Get(SectorKey(s) + "_answer")}</color></link>");
        return $"{Lang.Get("eye_question")}\n\n{string.Join("\n", lines)}";
    }

    public const int UnlockConditions = 2;

    public static bool GodAscensionSeen => GodEndings.AscensionIds.Any(Endings.Seen);
    public static bool MirrorEndingSeen => MirrorEndings.All.Any(e => Endings.Seen(e.Id));

    // 채운 해금 조건 수 (0~2)
    public static int UnlockProgress => (GodAscensionSeen ? 1 : 0) + (MirrorEndingSeen ? 1 : 0);
    public static bool Unlocked => UnlockProgress >= UnlockConditions;

    // 잠긴 버튼 제목: 조건 하나마다 금빛이 절반씩 차오른다 (조건이 둘뿐이라 신 모드처럼 "마지막 한 칸 남김"을 하지 않는다)
    public static float LockedFill(int progress) => (float)Math.Min(progress, UnlockConditions) / UnlockConditions;
    public static string LockedCount(int progress) => $"({Math.Min(progress, UnlockConditions)}/{UnlockConditions})";
}
