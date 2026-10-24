using System;

// 신 모드 v2 기프티드 효과 (v2g041~080). v2g066(매년 포인트)은 GiftedV2Table.cs에 이미 있어 여기서 등록하지 않는다.
// 새 훅은 GiftedV2GodHooks에 모았다(보유하지 않으면 전부 무변화·난수 없음). 부르는 곳은 각 훅 주석 참고.
public static partial class GiftedV2Table
{
    public const float EmberBonus = 0.10f; public const int EmberFails = 3;
    public const float ComboRewardScale = 1.05f;
    public const float BroadFaithGain = 1.5f;
    public const float WonderBonus = 0.10f, CommuneBonus = 0.10f, EventFavorBonus = 0.10f;
    public const float RevelationGain = 15f, HealingGain = 15f, ChanceEventGain = 15f, VesselEventLossCut = 20f;
    public const int FeastPointMilli = 150;            // 연회 성공 1회당 권능 포인트 0.15 (1000 모이면 1점)
    public const float YearlyStatPercent = 0.5f;

    static readonly string KProselytize = CultActions.Key(CultAction.Proselytize), KPreach = CultActions.Key(CultAction.Preach),
        KServe = CultActions.Key(CultAction.Serve), KDesire = CultActions.Key(CultAction.Desire), KPray = CultActions.Key(CultAction.Pray);

    static bool CultKey(CheckContext c, string key) => c.Kind == CheckKind.Cult && c.Key == key;
    static bool ActionKey(StatChange c, string key) => c.Source == ChangeSource.Action && c.Key == key;
    static int Hash(string s) => s?.GetHashCode() ?? 0;

    static partial void RegisterGod()
    {
        var R = (Action<string, EffectDef>)GiftedV2Effects.Register;
        var hook = new Func<EffectDef>(() => new EffectDef()); // 효과는 GiftedV2GodHooks 쪽 훅이 냄

        R("v2g041", hook()); // 휴식 1년: GiftedV2GodHooks.OnRestYear (CultFlow.RestRoutine)
        R("v2g042", new EffectDef
        {
            OnYearEnd = () =>
            {
                var cult = GameSession.Cult; var personal = GameSession.Personal;
                foreach (var id in CultStatus.VisibleStats) cult.AddWithRemainder(id, cult[id].Value * YearlyStatPercent / 100f);
                foreach (PersonalStatId id in Enum.GetValues(typeof(PersonalStatId)))
                    if (!PersonalStatInfo.IsHidden(id)) personal.AddWithRemainder(id, personal[id].Value * YearlyStatPercent / 100f);
            },
        });
        R("v2g043", new EffectDef
        {
            CheckBonusFn = _ => Counters.Get("gv2g043:fails", true) >= EmberFails ? EmberBonus : 0f,
            OnCheck = (_, ok) =>
            {
                var f = Counters.Get("gv2g043:fails", true);
                Counters.Set("gv2g043:fails", f >= EmberFails || ok ? 0 : f + 1, true); // 보너스를 받은 판정이면 결과와 무관하게 초기화
            },
        });
        R("v2g044", hook()); // UI: QuestionPanel.ChoiceSuffix 가 GiftedV2GodHooks.ShowsChoiceChance 를 읽는다
        R("v2g045", new EffectDef
        {
            OnCheck = (c, ok) =>
            {
                if (c.Kind == CheckKind.Event) return;
                var h = Hash(c.Key);
                if (!ok) { Counters.Set("gv2g045:streak", 0, true); return; }
                if (Counters.Get("gv2g045:key", true) == h) Counters.Add("gv2g045:streak", 1, true);
                else { Counters.Set("gv2g045:key", h, true); Counters.Set("gv2g045:streak", 1, true); }
            },
            StatMultiplierFn = c => c.Source == ChangeSource.Action && c.IsGain && Counters.Get("gv2g045:streak", true) >= 2
                && Counters.Get("gv2g045:key", true) == Hash(c.Key) ? ComboRewardScale : 1f,
        });
        R("v2g046", hook()); // UI: CultInfoPanel 이 GiftedV2GodHooks.ShowsEndingProgress 를 읽는다
        R("v2g047", new EffectDef
        {
            OnCheck = (c, _) =>
            {
                if (c.Kind != CheckKind.Event || c.Key == null) return;
                var key = "gv2g047:" + c.Key;
                if (Counters.Get(key) > 0) return;
                Counters.Set(key, 1);
                Counters.Add("gv2g047:n");
                if (Counters.Get("gv2g047:n") % 2 != 0) return; // 처음 겪는 이벤트 2번에 1점
                if (GameSession.GodMode && GameSession.CultFounded) GameSession.God.Points.Add(1);
            },
        });
        R("v2g048", hook()); // CultEvent.RollRandom → GiftedV2GodHooks.SuppressRepeatedEvent
        R("v2g049", new EffectDef { RescueFailure = _ => GiftedV2Effects.TryUseToken("v2g049") && Pass(HalfChance) }); // 토큰은 실패해도 소모
        R("v2g050", new EffectDef { GainPercent = BroadFaithGain });

        R("v2g051", new EffectDef { CheckBonus = WonderBonus, CheckWhen = c => CultKey(c, KProselytize) });
        R("v2g052", new EffectDef { GainPercent = RevelationGain, StatWhen = c => ActionKey(c, KPreach) });
        R("v2g053", new EffectDef { GainPercent = HealingGain, StatWhen = c => ActionKey(c, KServe) });
        R("v2g054", new EffectDef // 근사: 연회가 주는 포인트가 없어 성공마다 0.15점을 모아 정수로 지급
        {
            OnCheck = (c, ok) =>
            {
                if (!ok || !CultKey(c, KDesire) || !GameSession.GodMode || !GameSession.CultFounded) return;
                var milli = Counters.Get("gv2g054:milli", true) + FeastPointMilli;
                Counters.Set("gv2g054:milli", milli % 1000, true);
                if (milli >= 1000) GameSession.God.Points.Add(milli / 1000);
            },
        });
        R("v2g055", new EffectDef { CheckBonus = CommuneBonus, CheckWhen = c => CultKey(c, KPray) });
        R("v2g056", hook()); // GodBuildings.CostOf → GiftedV2GodHooks.BuildingCost
        for (int i = 57; i <= 61; i++) R("v2g0" + i, hook()); // GodBuildings.Floor → BuildingEffectScale
        R("v2g062", hook()); // GodPressure.AddHeat → HeatGainScale
        R("v2g063", hook());
        R("v2g064", hook()); // GodRules.Forced → ResistVesselLoss
        R("v2g065", hook()); // GodRules.AfterAction → VesselRecoveryBonus
        for (int i = 67; i <= 70; i++) R("v2g0" + i, hook()); // GodTree.Add → OnNodeLearned
        R("v2g071", new EffectDef { CheckBonus = EventFavorBonus, CheckWhen = c => c.Kind == CheckKind.Event && GiftedV2GodHooks.KindOf(c.Key) == "heaven" });
        R("v2g072", new EffectDef { CheckBonus = EventFavorBonus, CheckWhen = c => c.Kind == CheckKind.Event && GiftedV2GodHooks.KindOf(c.Key) == "earth" });
        R("v2g073", hook()); // GodRules.AfterEvent → EventHeatScale
        R("v2g074", new EffectDef { GainPercent = ChanceEventGain, StatWhen = c => c.Source == ChangeSource.Event && GiftedV2GodHooks.KindOf(c.Key) == "random" });
        R("v2g075", new EffectDef
        {
            LossPercent = VesselEventLossCut,
            StatWhen = c => c.Source == ChangeSource.Event && GiftedV2GodHooks.KindOf(c.Key) == "vessel" && Counters.Get("gv2g075:fail", true) == 1,
            OnCheck = (c, ok) => { if (c.Kind == CheckKind.Event) Counters.Set("gv2g075:fail", ok ? 0 : 1, true); },
        });
        R("v2g076", hook()); // GodSkill.Cost → AscensionCostEase
        R("v2g077", new EffectDef
        {
            OnYearEnd = () =>
            {
                var years = GameSession.Age.Years - GodRules.StartAgeYears;
                if (GameSession.GodMode && GameSession.CultFounded && years > 0 && years % 25 == 0) GameSession.God.Points.Add(1);
            },
        });
        R("v2g078", hook()); // GodBuildings.CostOf → BuildingCost
        R("v2g079", hook()); // GodPressure.AddHeat → HeatGainScale
        R("v2g080", hook()); // GodRules.DueEnding → BeforeAscension
    }
}

// 신 모드 v2 기프티드가 게임 코드에서 부르는 훅. 보유(+신 모드)가 아니면 무변화.
public static class GiftedV2GodHooks
{
    public const float EnvyCut = 0.20f, SuspicionCut = 0.20f, SanctuaryCut = 0.10f, IndulgenceCut = 0.25f;
    public const float SeizureResist = 0.20f, VesselRecoveryUp = 0.25f, RepeatSkip = 0.25f;
    public const float ConsecrationCostCut = 0.15f, BuildingCostCut = 0.10f, BuildingEffectUp = 0.20f;
    public const int RestYearHeat = 1, NodeCutMilli = 100; // 노드 비용 10%: 비용 1점당 0.1점을 돌려줌 (정수 비용이라 환급으로 근사)

    static bool On(string id) => GameSession.GodMode && GiftedV2Effects.IsActive(id);

    // 이벤트 id 접두어 → 종류 (GodCounters.KindOfId와 같은 분류). id가 없으면 null
    public static string KindOf(string id) => id == null ? null : id.StartsWith("hv", StringComparison.Ordinal) ? "heaven"
        : id.StartsWith("ea", StringComparison.Ordinal) ? "earth" : id.StartsWith("vs", StringComparison.Ordinal) ? "vessel"
        : id.StartsWith("dl", StringComparison.Ordinal) ? "random" : "indulge";

    // UI: 선택지 성공 확률 표시 (v2g044) / 엔딩 진행도 표시 (v2g046). 선택지 창·상태 창이 읽는다.
    public static bool ShowsChoiceChance => On("v2g044");
    public static bool ShowsEndingProgress => On("v2g046");

    // 휴식 1년을 마쳤을 때 (CultFlow.RestRoutine): 시기·의혹 −2
    public static void OnRestYear()
    {
        if (On("v2g041")) GameSession.God.Pressure.AddHeat(-RestYearHeat, -RestYearHeat);
    }

    // 시기(heaven)·의혹 상승분에 곱한다 (GodPressure.AddHeat)
    public static float HeatGainScale(bool heaven)
    {
        if (!GameSession.GodMode) return 1f;
        float cut = 0;
        if (GiftedV2Effects.IsActive(heaven ? "v2g062" : "v2g063")) cut += heaven ? EnvyCut : SuspicionCut;
        if (GiftedV2Effects.IsActive("v2g079") && GameSession.God.Buildings.LevelOf(GodBuildingType.Temple) > 0) cut += SanctuaryCut;
        return Math.Max(0f, 1f - cut);
    }

    // 이벤트 결과가 주는 시기·의혹에 곱한다 (GodRules.AfterEvent)
    public static float EventHeatScale(string eventId) => eventId != null && KindOf(eventId) == "indulge" && On("v2g073") ? 1f - IndulgenceCut : 1f;

    // 승천 엔딩이 나오기 직전 한 번 (GodRules.DueEnding): 시기·의혹 절반
    public static void BeforeAscension()
    {
        if (!On("v2g080") || !GiftedV2Effects.TryUseToken("v2g080")) return;
        var p = GameSession.God.Pressure;
        p.Restore(p.Envy * 0.5f, p.Suspicion * 0.5f);
    }

    // 그릇이 바닥나 빼앗길 판정 (GodRules.Forced): true면 막아 체력·정신을 1로 남김. 판정은 한 번만 굴린다.
    public static bool ResistVesselLoss(PersonalStatus personal)
    {
        if (!On("v2g064") || Counters.Get("gv2g064:lost", true) == 1) return false;
        if (GameSession.Rng.NextDouble() >= SeizureResist) { Counters.Set("gv2g064:lost", 1, true); return false; }
        foreach (var id in new[] { PersonalStatId.Health, PersonalStatId.Mentality })
            if (personal[id].Value < 1) personal[id].Set(1);
        return true;
    }

    // 향연 그릇 회복 배율에 더한다 (GodRules.AfterAction)
    public static float VesselRecoveryBonus => On("v2g065") ? VesselRecoveryUp : 0f;

    // 축성 비용 (GodBuildings.CostOf): 축성 −15%, 건물 −10% 합산
    public static int BuildingCost(int cost)
    {
        float cut = (On("v2g056") ? ConsecrationCostCut : 0f) + (On("v2g078") ? BuildingCostCut : 0f);
        return cut <= 0 ? cost : Math.Max(1, (int)Math.Round(cost * (1f - cut)));
    }

    // 건물 효과(방어 하한) 배율 (GodBuildings.Floor)
    public static float BuildingEffectScale(GodBuildingType type)
    {
        var id = type switch
        {
            GodBuildingType.Statue => "v2g057", GodBuildingType.Temple => "v2g058", GodBuildingType.Library => "v2g059",
            GodBuildingType.Village => "v2g060", _ => "v2g061",
        };
        return On(id) ? 1f + BuildingEffectUp : 1f;
    }

    // 노드를 익힐 때 (GodTree.Add): 갈래별 노드 비용 −10%를 환급 포인트로 지급
    public static void OnNodeLearned(GodSkill skill, bool free)
    {
        if (free || !GameSession.GodMode) return;
        var id = skill.Branch switch
        {
            GodBranch.Miracle => "v2g067", GodBranch.Prophecy => "v2g068", GodBranch.Mercy => "v2g069", GodBranch.Desire => "v2g070", _ => null,
        };
        if (id == null || !GiftedV2Effects.IsActive(id)) return;
        var milli = Counters.Get("gv2g067:milli", true) + skill.Cost * NodeCutMilli;
        Counters.Set("gv2g067:milli", milli % 1000, true);
        if (milli >= 1000) GameSession.God.Points.Add(milli / 1000);
    }

    // 승천 노드 비용 완화 (GodSkill.Cost): 두 핵이 열렸으면 1점 덜
    public static int AscensionCostEase => On("v2g076") && GameSession.God.Tree.AscensionOpen ? 1 : 0;

    // 소진한 풀에서 이미 나온 이벤트를 다시 뽑는 것을 절반으로 줄임 (CultEvent.RollRandom). true면 이번엔 이벤트 없음.
    public static bool SuppressRepeatedEvent(Random rng) => GiftedV2Effects.IsActive("v2g048") && rng.NextDouble() < RepeatSkip;
}
