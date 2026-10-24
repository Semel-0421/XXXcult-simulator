using System;
using System.Collections.Generic;
using System.Linq;

// 일반 모드 v2 기프티드(v2n001~040) 효과 등록. v2n001은 GiftedV2Table.cs의 예시로 이미 등록돼 있어 여기서 빼 둠.
// 훅 대응표: Docs/GDD/systems/SYS_GIFTED_V2_EFFECTS.md, 추가 훅은 GiftedV2NormalHooks.cs
public static partial class GiftedV2Table
{
    public const int RescueChoiceChancePercent = 50, RescueReputation = 10, RescueOfferings = 10;
    public const float WideCircleEventGain = 2.5f, ChroniclerDoubleChance = 0.025f, NormalCheckBonus = 0.08f;
    public const int RerollsPerRun = 2, NormalGraceTurns = 4, NormalReputationRestore = 100, NormalStartPersonalBonus = 2, LastSermonPercent = 10, LongViewYears = 5;
    public const float MisfortuneReducePercent = 7.5f, GiftedGrantBonusPercent = 5f, DoctrineCostReducePercent = 10f;
    public const float TwoFacesMaxBonus = 0.03f, TwoFacesGapRange = 50f, ExtremeAverage = 80f, ExtremeBonus = 1.05f;
    public const float PersecutionLossScale = 0.75f, NormalDecayHalf = 0.5f, SlowAgingScale = 0.75f;
    public const string NextRunBonusKey = "gv2:v2n040:nextrun", NormalGraceKey = "gv2:v2n010:until";

    static bool nChronicler;
    static HashSet<string> nBribe, nAdult;

    static readonly PersonalStatId[] NDesire = { PersonalStatId.Lust, PersonalStatId.Greed, PersonalStatId.Appetite, PersonalStatId.Ambition };
    static readonly PersonalStatId[] NVirtue = { PersonalStatId.Temperance, PersonalStatId.Charity, PersonalStatId.Patience, PersonalStatId.Integrity };

    static bool NAct(StatChange c, params string[] keys) => c.Source == ChangeSource.Action && Array.IndexOf(keys, c.Key) >= 0;
    static bool NIsTrait(PersonalStatId? p) => p is { } s && (Array.IndexOf(NDesire, s) >= 0 || Array.IndexOf(NVirtue, s) >= 0);

    static void NAddAll(int amount)
    {
        if (amount == 0) return;
        foreach (PersonalStatId id in Enum.GetValues(typeof(PersonalStatId))) GameSession.Personal[id].Add(amount);
    }

    static bool NHas(string text, params string[] words) => text != null && words.Any(w => text.Contains(w));

    static HashSet<string> NBribeEvents()
    {
        if (nBribe != null) return nBribe;
        nBribe = new HashSet<string>();
        foreach (var e in CultEvents.All)
            if (e.Question.Choices.Any(ch => NHas(ch.Text, "매수", "뇌물", "Bribe", "bribe"))) nBribe.Add(e.Id);
        foreach (var e in LifeEvents.All)
            if (e.Question.Choices.Any(ch => NHas(ch.Text, "매수", "뇌물", "Bribe", "bribe"))) nBribe.Add(e.Id);
        return nBribe;
    }

    static bool NIsAdult(string id)
    {
        nAdult ??= new HashSet<string>(AdultEvents.All.Select(e => e.Id));
        return id != null && nAdult.Contains(id);
    }

    static float NGap() => Math.Abs(CultEvent.TraitAverage(GameSession.Personal, CultEvent.MoodTrait.Desire) - CultEvent.TraitAverage(GameSession.Personal, CultEvent.MoodTrait.Virtue));

    static Choice NRescue() => GameSession.Rng.Next(100) < RescueChoiceChancePercent
        ? new Choice("뜻밖의 은인에게 손을 내민다\u001FReach out to the unexpected patron",
            new Outcome("어디선가 낯선 이가 나타나 위기를 수습해 줍니다.\u001FA stranger appears from nowhere and settles the crisis.").C(StatId.Reputation, RescueReputation).C(StatId.Offerings, RescueOfferings))
        : null;

    static void NReg(string id, EffectDef def) => GiftedV2Effects.Register(id, def);

    static partial void RegisterNormal()
    {
        // 공유 10 (v2n001은 GiftedV2Table.RegisterAll)
        NReg("v2n002", new EffectDef { PreventDeath = () => GiftedV2Effects.TryUseToken("v2n002") && Pass(HalfChance) });
        NReg("v2n003", new EffectDef { GainPercent = WideCircleEventGain, StatWhen = c => c.Source == ChangeSource.Event });
        NReg("v2n004", new EffectDef { RescueChoiceNormal = NRescue });
        // 나이로 인한 스탯 하락 로직이 게임에 없어, 행동·이벤트가 아닌 자연 감소(출처 Other)로 근사
        NReg("v2n005", new EffectDef { StatMultiplierFn = c => c.Personal == PersonalStatId.Health && !c.IsGain && c.Source == ChangeSource.Other ? SlowAgingScale : 1f });
        NReg("v2n006", new EffectDef { GiftedGrantBonusNormal = GiftedGrantBonusPercent });
        NReg("v2n007", new EffectDef { MisfortuneGainReduceNormal = MisfortuneReducePercent }); // 신 모드 압력(GodPressure)은 신 쪽 배선 필요
        NReg("v2n008", new EffectDef
        {
            OnCheck = (c, ok) => nChronicler = ok && GameSession.Rng.NextDouble() < ChroniclerDoubleChance,
            StatMultiplierFn = c => nChronicler && c.IsGain && c.Source != ChangeSource.Other ? 2f : 1f,
        });
        NReg("v2n009", new EffectDef
        {
            RescueFailure = c => GiftedV2Effects.TryUseToken("v2n009", RerollsPerRun)
                && GameSession.Rng.NextDouble() < (c.Chance < 0f ? ActionCheck.BaseChance : c.Chance),
        });
        // 되돌리기 UI가 없어 근사: 최종·극단 엔딩이 걸려도 한 번 GraceTurns턴 유예
        NReg("v2n010", new EffectDef
        {
            PreventEnding = e =>
            {
                if (!(e.IsExtreme || e.Final != null)) return false;
                var now = GameSession.CultTime.Turns;
                var until = Counters.Get(NormalGraceKey, perRun: true);
                if (until > 0) return now < until;
                if (!GiftedV2Effects.TryUseToken("v2n010") || !Pass(HalfChance)) return false; // 토큰은 실패해도 소모
                Counters.Set(NormalGraceKey, now + NormalGraceTurns, perRun: true);
                return true;
            },
        });

        // 전용 30
        NReg("v2n011", new EffectDef { OnCheck = (c, ok) => { if (ok && c.Kind == CheckKind.Life && c.Key == nameof(LifeAction.Exercise)) GameSession.Personal[PersonalStatId.Health].Add(1); } });
        NReg("v2n012", new EffectDef { CheckBonus = NormalCheckBonus, CheckWhen = c => c.Kind == CheckKind.Life && c.Key == nameof(LifeAction.Meditate) });
        NReg("v2n013", new EffectDef { CheckBonus = NormalCheckBonus, CheckWhen = c => c.Kind == CheckKind.Life && c.Key == nameof(LifeAction.Study) });
        NReg("v2n014", new EffectDef { CheckBonus = NormalCheckBonus, CheckWhen = c => c.Kind == CheckKind.Life && c.Key == nameof(LifeAction.Socialize) });
        NReg("v2n015", new EffectDef { GainPercent = 20, StatWhen = c => NAct(c, "Pray", "CultPray") });
        NReg("v2n016", new EffectDef { GainPercent = 15, StatWhen = c => NAct(c, "Proselytize") && c.Cult == StatId.Followers });
        NReg("v2n017", new EffectDef { GainPercent = 15, StatWhen = c => NAct(c, "Preach") && c.Cult == StatId.FollowerFaith });
        NReg("v2n018", new EffectDef { GainPercent = 15, StatWhen = c => NAct(c, "Serve") && c.Cult == StatId.Reputation });
        NReg("v2n019", new EffectDef { GainPercent = 15, StatWhen = c => NAct(c, "Desire") && c.Cult == StatId.Offerings });
        NReg("v2n020", new EffectDef { GainPercent = 15, StatWhen = c => NAct(c, "CultPray") && c.Cult == StatId.LeaderFaith });
        // 일반 모드에 봉헌(축성) 행동이 없어 근사: 헌금(Offerings) 증가 전부 +20%
        NReg("v2n021", new EffectDef { GainPercent = 20, StatWhen = c => c.Cult == StatId.Offerings });
        NReg("v2n022", new EffectDef { LossPercent = 20, StatWhen = c => c.Cult == StatId.Authority });
        NReg("v2n023", new EffectDef { LossPercent = 20, StatWhen = c => c.Cult == StatId.Offerings });
        NReg("v2n024", new EffectDef { LossPercent = 20, StatWhen = c => c.Cult == StatId.Followers }); // 쇠퇴·연말 이탈은 DecayMultiplierNormal 경유
        NReg("v2n025", new EffectDef { DoctrineCostReducePercentNormal = DoctrineCostReducePercent });
        NReg("v2n026", new EffectDef { StatMultiplierFn = c => c.Cult == StatId.Reputation && !c.IsGain && c.Source == ChangeSource.Other ? NormalDecayHalf : 1f });
        NReg("v2n027", new EffectDef
        {
            StatMultiplierFn = c => c.Cult != null && !c.IsGain && SocialStanding.Of(GameSession.Cult) == Standing.Persecuted ? PersecutionLossScale : 1f,
        });
        NReg("v2n028", new EffectDef { GainPercent = 15, StatWhen = c => c.Personal == PersonalStatId.Wealth });
        NReg("v2n029", new EffectDef { GainPercent = 3, StatWhen = c => c.Personal == PersonalStatId.Piety && GameSession.Personal[PersonalStatId.Wealth].Value == 0 });
        // 뇌물 선택지가 있는 이벤트에서 '거절/Refuse/Decline' 선택지를 고르면 청렴 +2
        NReg("v2n030", new EffectDef
        {
            OnCheck = (c, ok) =>
            {
                if (c.Kind != CheckKind.Event || c.Choice == null || c.Key == null || !NBribeEvents().Contains(c.Key)) return;
                if (NHas(c.Choice.Text, "거절", "Refuse", "Decline")) GiftedEffects.Apply(GameSession.Personal, PersonalStatId.Integrity, 2);
            },
        });
        NReg("v2n031", new EffectDef { StatMultiplierFn = c => c.IsGain ? 1f + TwoFacesMaxBonus * Math.Max(0f, 1f - NGap() / TwoFacesGapRange) : 1f });
        NReg("v2n032", new EffectDef
        {
            StatMultiplierFn = c =>
            {
                if (!c.IsGain || c.Personal is not { } s) return 1f;
                var desire = Array.IndexOf(NDesire, s) >= 0;
                if (!desire && Array.IndexOf(NVirtue, s) < 0) return 1f;
                var avg = CultEvent.TraitAverage(GameSession.Personal, desire ? CultEvent.MoodTrait.Desire : CultEvent.MoodTrait.Virtue);
                return avg >= ExtremeAverage ? ExtremeBonus : 1f;
            },
        });
        NReg("v2n033", new EffectDef { LossPercent = 15, StatWhen = c => NIsTrait(c.Personal) });
        NReg("v2n034", new EffectDef { GainPercent = 10, StatWhen = c => c.Source == ChangeSource.Event && NIsAdult(c.Key) });
        // 나이로 인한 평판 하락 로직이 없어 근사: 자연 감소(출처 Other)로 인한 명망 하락 없음
        NReg("v2n035", new EffectDef { LossPercent = 100, StatWhen = c => c.Cult == StatId.Reputation && c.Source == ChangeSource.Other });
        NReg("v2n036", new EffectDef { DoctrinePointBonusNormal = years => years / LongViewYears });
        NReg("v2n037", new EffectDef
        {
            PreventEnding = e =>
            {
                if (e.Id != Endings.Dissolved || !GiftedV2Effects.TryUseToken("v2n037")) return false;
                var rep = GameSession.Cult[StatId.Reputation];
                rep.Set(Math.Max(rep.Value, NormalReputationRestore));
                return true;
            },
        });
        NReg("v2n038", new EffectDef { OnRunStart = () => NAddAll(NormalStartPersonalBonus) });
        NReg("v2n039", new EffectDef
        {
            OnEnding = e =>
            {
                if (!Progress.IsDeath(e) || !GameSession.CultFounded) return;
                var followers = GameSession.Cult[StatId.Followers];
                followers.Add(followers.Value * LastSermonPercent / 100);
            },
        });
        NReg("v2n040", new EffectDef
        {
            OnEnding = e => { if (!Endings.Seen(e.Id)) Counters.Add(NextRunBonusKey); },
            OnRunStart = () =>
            {
                var bonus = Counters.Get(NextRunBonusKey);
                if (bonus <= 0) return;
                Counters.Set(NextRunBonusKey, 0);
                NAddAll(bonus);
            },
        });
    }
}
