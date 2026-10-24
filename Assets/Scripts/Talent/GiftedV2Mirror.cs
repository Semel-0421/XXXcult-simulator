using System;
using System.Collections.Generic;

// 거울 세계 v2 기프티드(v2m081~v2m120) 효과. 판정·스탯 변화는 EffectDef 훅으로, 그 밖의 규칙 변경은 아래 GiftedV2Mirror 훅으로 게임 쪽에서 부른다.
// 보유하지 않으면 GiftedV2Mirror 훅은 모두 무변화(1, 0, false)이고 난수도 쓰지 않는다.
public static partial class GiftedV2Table
{
    public const float ExtraActionChance = 0.05f;
    public const float AttritionLossPercent = 20f, DeclineLossPercent = 5f, OfferingsFloorLossPercent = 50f, LoseChoiceLossPercent = 10f;
    public const int OfferingsFloor = 300;
    public const float RecoveryGainPercent = 25f, SpecialGainPercent = 15f, LateGainPercent = 5f, EarlyGainPercent = 10f, BalanceGainPercent = 5f, AdversityOfferingsPercent = 15f;
    public const int LateYears = 20, EarlyYears = 5, BalanceAverage = 50, AdversityMisfortune = 6;
    public const int FailsForSureSuccess = 10, FailsForStat = 10, ChainMax = 3;
    public const float ChainBonusPercent = 2.5f;

    static bool Cult => GameSession.CultFounded;
    static int CultYears => GameSession.CultTime.Years;
    static float Avg(CultEvent.MoodTrait t) => CultEvent.TraitAverage(GameSession.Personal, t);
    static bool IsRecoveryEvent(string id) => id != null && (id.StartsWith("mv") || id.StartsWith("mh"));
    static bool IsSpecialEvent(string id) => id != null && id.StartsWith("ms");

    static int MHash(string s) { int h = 17; foreach (var ch in s ?? "") h = h * 31 + ch; return h; }

    static partial void RegisterMirror()
    {
        var reg = (Action<string, EffectDef>)GiftedV2Effects.Register;
        var marker = new Func<EffectDef>(() => new EffectDef()); // 규칙 쪽(GiftedV2Mirror 훅)에서 처리하는 효과: 등록만 해 둔다

        // ---- 공유 (081~090) ----
        reg("v2m081", marker()); // Turns.Life/Cult → GiftedV2Mirror.GrantsExtraAction
        reg("v2m082", new EffectDef
        {
            StatMultiplierFn = c =>
            {
                if (c.IsGain || c.Amount == 0) return 1f;
                int cur, min;
                if (c.Cult is { } s) { var st = GameSession.Cult[s]; cur = st.Value; min = st.Min; }
                else if (c.Personal is { } p) { var st = GameSession.Personal[p]; cur = st.Value; min = st.Min; }
                else return 1f;
                if (cur <= min || cur + c.Amount > min) return 1f;
                if (!GiftedV2Effects.TryUseToken("v2m082") || !Pass(HalfChance)) return 1f; // 토큰은 실패해도 소모
                return (cur - min - 1) / (float)-c.Amount; // 0이 되는 하락은 1이 남도록 줄인다
            },
        });
        reg("v2m083", new EffectDef
        {
            OnCheck = (c, ok) => { if (c.Kind == CheckKind.Event) Counters.Set("gv2m083_lose", ok ? 0 : 1, perRun: true); },
            StatMultiplierFn = c => c.Source == ChangeSource.Event && !c.IsGain && Counters.Get("gv2m083_lose", perRun: true) > 0 ? 1f - LoseChoiceLossPercent / 100f : 1f,
        });
        reg("v2m084", marker()); // Rest.ApplyTurn → RestMultiplier
        reg("v2m085", new EffectDef { LossPercent = DeclineLossPercent, StatWhen = c => c.Source == ChangeSource.Other }); // Misfortune/SocialStanding 쇠퇴가 StatMultiplier를 거침
        reg("v2m086", marker()); // UI: GiftedV2Mirror.ShowsChoicePreview
        reg("v2m087", new EffectDef
        {
            RescueFailure = _ => Counters.Get("gv2m087_ready", perRun: true) > 0,
            OnCheck = (c, ok) =>
            {
                if (Counters.Get("gv2m087_ready", perRun: true) > 0) { Counters.Set("gv2m087_ready", 0, perRun: true); return; }
                if (ok) return;
                Counters.Add("gv2m087_fails", 1, perRun: true);
                if (Counters.Get("gv2m087_fails", perRun: true) % FailsForSureSuccess == 0) Counters.Set("gv2m087_ready", 1, perRun: true);
            },
        });
        reg("v2m088", marker()); // SocialStanding.YearEnd → YearlyRecruits
        reg("v2m089", marker()); // GiftedV2Mirror.TryRerollEvent (UI가 부른다)
        reg("v2m090", new EffectDef
        {
            OnCheck = (c, ok) =>
            {
                if (c.Kind == CheckKind.Event) return;
                var h = MHash(c.Key);
                var same = Counters.Get("gv2m090_key", perRun: true) == h && Counters.Has("gv2m090_key");
                Counters.Set("gv2m090_rep", same ? Math.Min(ChainMax, Counters.Get("gv2m090_rep", perRun: true) + 1) : 0, perRun: true);
                Counters.Set("gv2m090_key", h, perRun: true);
            },
            StatMultiplierFn = c => c.Source == ChangeSource.Action && c.IsGain ? 1f + ChainBonusPercent / 100f * Counters.Get("gv2m090_rep", perRun: true) : 1f,
        });

        // ---- 전용 (091~120) ----
        reg("v2m091", marker()); // YearlyRecruits
        reg("v2m092", marker()); // CultScore → FollowerReputationRate
        reg("v2m093", marker()); // CultScore → ReputationOfferingsRate
        reg("v2m094", marker()); // GameSession.AddMisfortune → AllowsMisfortuneGain
        reg("v2m095", new EffectDef { OnYearEnd = () => { if (Cult && GameSession.Misfortune >= 6) GameSession.ReduceMisfortuneByGifted(1); } });
        reg("v2m096", marker()); // MirrorRules.FollowersGone → GraceTurnsBonus
        reg("v2m097", new EffectDef
        {
            OnYearEnd = () =>
            {
                if (!Cult) return;
                var gap = Math.Abs(MirrorRules.Tenths(Avg(CultEvent.MoodTrait.Desire)) - MirrorRules.Tenths(Avg(CultEvent.MoodTrait.Virtue)));
                if (gap <= 100) GameSession.ReduceMisfortuneByGifted(1);
            },
        });
        reg("v2m098", marker()); // YearlyRecruits
        reg("v2m099", marker()); // AllowsMisfortuneGain
        reg("v2m100", marker()); // GameSession.AdvanceTurn → GiftedV2Mirror.AfterTurn
        reg("v2m101", marker()); // YearlyRecruits
        reg("v2m102", marker()); // DoctrinePoints.Earned → DoctrinePointsBonus
        reg("v2m103", marker()); // DoctrineTree.PrerequisitesMet → WaivesFinalPrerequisite
        reg("v2m104", marker()); // DoctrinePointsBonus
        reg("v2m105", marker()); // DoctrineTree.BuildEffects → DoctrineEffectScale
        reg("v2m106", marker()); // DoctrineEffectScale
        // v2m107은 GiftedV2Table.cs의 예시
        reg("v2m108", new EffectDef { LossPercent = OfferingsFloorLossPercent, StatWhen = c => c.Cult == StatId.Offerings && GameSession.Cult[StatId.Offerings].Value <= OfferingsFloor });
        reg("v2m109", new EffectDef { LossPercent = AttritionLossPercent, StatWhen = c => c.Cult == StatId.Followers });
        reg("v2m110", new EffectDef { GainPercent = RecoveryGainPercent, StatWhen = c => c.Source == ChangeSource.Event && IsRecoveryEvent(c.Key) });
        reg("v2m111", new EffectDef { GainPercent = SpecialGainPercent, StatWhen = c => c.Source == ChangeSource.Event && IsSpecialEvent(c.Key) });
        reg("v2m112", new EffectDef { GainPercent = LateGainPercent, StatWhen = c => Cult && CultYears >= LateYears });
        reg("v2m113", new EffectDef { GainPercent = EarlyGainPercent, StatWhen = c => Cult && CultYears < EarlyYears });
        reg("v2m114", new EffectDef
        {
            GainPercent = BalanceGainPercent,
            StatWhen = c => Avg(CultEvent.MoodTrait.Desire) >= BalanceAverage && Avg(CultEvent.MoodTrait.Virtue) >= BalanceAverage,
        });
        reg("v2m115", new EffectDef { GainPercent = AdversityOfferingsPercent, StatWhen = c => c.Cult == StatId.Offerings && GameSession.Misfortune >= AdversityMisfortune });
        reg("v2m116", new EffectDef { OnRunStart = GiftedV2Mirror.GrantDoubleMirrorStart });
        reg("v2m117", new EffectDef
        {
            OnCheck = (c, ok) =>
            {
                if (ok) return;
                Counters.Add("gv2m117_fails", 1, perRun: true);
                if (Counters.Get("gv2m117_fails", perRun: true) % FailsForStat == 0) GiftedV2Mirror.RaiseLowestTrait();
            },
        });
        reg("v2m118", marker()); // AllowsMisfortuneGain
        reg("v2m119", new EffectDef
        {
            OnEnding = e => { if (MirrorRules.Find(e.Id) != null && !Endings.Seen(e.Id)) Counters.Add(GiftedV2Mirror.NextPointsKey, GiftedV2Mirror.NewEndingPoints); },
            OnRunStart = () =>
            {
                var pending = Counters.Get(GiftedV2Mirror.NextPointsKey);
                if (pending <= 0) return;
                Counters.Set(GiftedV2Mirror.NextPointsKey, 0);
                Counters.Set(GiftedV2Mirror.RunPointsKey, pending, perRun: true);
            },
        });
        reg("v2m120", new EffectDef
        {
            PreventEnding = e =>
            {
                if (e.Id != "inv_end_absorbed" && e.Id != "inv_end_followers") return false;
                if (!GiftedV2Effects.TryUseToken("v2m120")) return false;
                if (e.Id == "inv_end_absorbed") GameSession.Cult[StatId.Reputation].Set(GiftedV2Mirror.SurviveReputation);
                else GameSession.Cult[StatId.Followers].Set(GiftedV2Mirror.SurviveFollowers);
                return true;
            },
        });
    }
}

// 게임 규칙 쪽에서 부르는 거울 전용 훅. 보유(+모드 적용)하지 않으면 무변화.
public static class GiftedV2Mirror
{
    public const string NextPointsKey = "gv2m119_next", RunPointsKey = "gv2m119_run";
    public const int NewEndingPoints = 2, SurviveReputation = 200, SurviveFollowers = 10, RestoredFollowers = 10, StartTraitBonus = 4, GraceBonusTurns = 4;
    public const float OneBranchScale = 1.15f, TwoBranchScale = 1.05f, ConversionScale = 1.10f, SettlementScale = 1.025f;

    static readonly Dictionary<string, GiftedV2Catalog.Entry> entries = new();
    static readonly Dictionary<string, bool> _onCache = new();
    static int _onCacheHash;

    static int ComputeOnHash()
    {
        var mode = GameSession.GodMode ? "god" : GameSession.MirrorMode ? "mirror" : "normal";
        var ownership = Achievements.V2OwnershipOn ? 1 : 0;
        var adult = GiftedV2Effects.AdultEnabled() ? 1 : 0;
        var unlockedCount = Profile.UnlockedCount;
        var forced = Achievements.ForceGiftedV2 switch { null => 0, true => 1, false => 2 }; // 시뮬 배치(전부/없음)가 바뀌면 다시 계산
        return (((mode.GetHashCode() * 31 + ownership) * 31 + adult) * 31 + unlockedCount) * 31 + forced;
    }

    public static bool On(string id)
    {
        if (!Achievements.V2OwnershipOn) return false;
        var hash = ComputeOnHash();
        if (_onCacheHash != hash) { _onCache.Clear(); _onCacheHash = hash; }
        if (_onCache.TryGetValue(id, out var cached)) return cached;
        if (!entries.TryGetValue(id, out var e)) entries[id] = e = GiftedV2Catalog.Find(id);
        var result = GiftedV2Effects.IsActive(e);
        _onCache[id] = result;
        return result;
    }

    public static void InvalidateOnCache() => _onCache.Clear();

    // v2m086 (UI 전용)
    public static bool ShowsChoicePreview => On("v2m086");

    // v2m081: 성공한 행동 뒤 5% 확률로 턴을 쓰지 않는다 (Turns.Life/Cult가 true면 시간이 흐르지 않게 일찍 끝냄)
    public static bool GrantsExtraAction(bool success) =>
        success && On("v2m081") && GameSession.Rng.NextDouble() < GiftedV2Table.ExtraActionChance;

    // v2m084: 휴식 회복량 배율
    public static float RestMultiplier() => On("v2m084") ? 1.125f : 1f;

    // v2m092 / v2m093: CultScore 환산 배율
    public static float FollowerReputationRate() => On("v2m092") ? ConversionScale : 1f;
    public static float ReputationOfferingsRate() => On("v2m093") ? ConversionScale : 1f;

    // v2m094 / v2m099 / v2m118: 불운 한 겹이 쌓일지 (GameSession.AddMisfortune이 부름)
    public static bool AllowsMisfortuneGain()
    {
        float keep = 1f;
        if (On("v2m094")) keep *= 0.8f;
        if (On("v2m099") && CultEvent.TraitAverage(GameSession.Personal, CultEvent.MoodTrait.Virtue) >= 60f) keep *= 0.75f;
        if (On("v2m118") && GameSession.CultFounded && GameSession.CultTime.Years >= 30) keep = 0f;
        if (keep >= 1f) return true;
        return keep > 0f && GameSession.Rng.NextDouble() < keep;
    }

    // v2m096: 신도 0 유예 턴 가산
    public static int GraceTurnsBonus() => On("v2m096") ? GraceBonusTurns : 0;

    // 연 1회 입교 조정 (SocialStanding.YearEnd): 088 정산 +5%, 091 손실 -2, 098/101 +1, 감소는 신도 이탈 배율을 거침
    public static float YearlyRecruits(int raw, int reputation)
    {
        float r = raw;
        if (raw < 0 && On("v2m091")) r = Math.Min(0, raw + 2);
        if (On("v2m098") && CultEvent.TraitAverage(GameSession.Personal, CultEvent.MoodTrait.Desire) >= 60f) r += 1;
        if (On("v2m101") && reputation >= 400) r += 1;
        if (r > 0 && On("v2m088")) r *= SettlementScale;
        if (r < 0) r *= GiftedV2Effects.StatMultiplier(StatId.Followers, -1);
        return r;
    }

    // v2m100: 턴이 끝난 뒤 신도가 0이면 1회 10명 (붕괴 판정이 시작되는 교단 5년 이후)
    public static void AfterTurn()
    {
        if (!On("v2m100") || GameSession.CultTime.Years < MirrorRules.FollowersFromYears) return;
        if (GameSession.Cult[StatId.Followers].Value > 0 || !GiftedV2Effects.TryUseToken("v2m100")) return;
        GameSession.Cult[StatId.Followers].Set(RestoredFollowers);
    }

    // v2m102 / v2m104 / v2m119: 교리 포인트 가산 (DoctrinePoints.Earned)
    public static int DoctrinePointsBonus(int cultYears, int baseEarned)
    {
        if (!Achievements.V2OwnershipOn || !GameSession.MirrorMode) return 0;
        int bonus = 0;
        if (On("v2m102")) bonus += cultYears;
        if (On("v2m119")) bonus += Counters.Get(RunPointsKey, perRun: true);
        if (On("v2m104")) bonus += (int)Math.Floor((baseEarned + bonus) * 0.10);
        return bonus;
    }

    // v2m103: 최종 스킬 선행 조건 하나(바로 앞 단계) 면제
    public static bool WaivesFinalPrerequisite() => On("v2m103");

    // v2m105 / v2m106: 배운 가지 수에 따른 교리 효과 배율 (1 가지 +15%, 2 가지 각 +5%)
    public static float DoctrineEffectScale(int learnedBranches)
    {
        if (learnedBranches == 1 && On("v2m105")) return OneBranchScale;
        if (learnedBranches == 2 && On("v2m106")) return TwoBranchScale;
        return 1f;
    }

    // v2m089: 2년에 1회 이벤트 다시 뽑기. UI가 CanRerollEvent로 버튼을 켜고 RerollCultEvent를 부른다
    public static bool CanRerollEvent => On("v2m089") && (Counters.Get("gv2m089_year", perRun: true) == 0 || GameSession.Age.Years + 1 - Counters.Get("gv2m089_year", perRun: true) >= 2); // 2년에 1번

    public static CultEvent RerollCultEvent()
    {
        if (!CanRerollEvent) return null;
        Counters.Set("gv2m089_year", GameSession.Age.Years + 1, perRun: true);
        return Turns.NextCultEvent();
    }

    static readonly PersonalStatId[] Traits =
    {
        PersonalStatId.Lust, PersonalStatId.Greed, PersonalStatId.Appetite, PersonalStatId.Ambition,
        PersonalStatId.Temperance, PersonalStatId.Charity, PersonalStatId.Patience, PersonalStatId.Integrity,
    };

    // v2m116: 일반 모드에서 최종 스킬을 익힌 적이 있으면 시작 때 욕망·미덕 8개 +4
    public static void GrantDoubleMirrorStart()
    {
        foreach (var branch in new[] { "Lust", "Greed", "Appetite", "Ambition", "Temperance", "Charity", "Patience", "Integrity" })
            if (Counters.Get("final_branch_" + branch) > 0)
            {
                foreach (var id in Traits) GameSession.Personal[id].Add(StartTraitBonus);
                return;
            }
    }

    // v2m117: 가장 낮은 욕망·미덕 스탯 +1
    public static void RaiseLowestTrait()
    {
        var lowest = Traits[0];
        foreach (var id in Traits) if (GameSession.Personal[id].Value < GameSession.Personal[lowest].Value) lowest = id;
        GameSession.Personal[lowest].Add(1);
    }
}
