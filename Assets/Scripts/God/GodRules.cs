using System;
using System.Collections.Generic;
using System.Linq;

// 이벤트 결과에만 쓰는 신 모드 효과 (Docs/god_choices.csv의 Envy Suspicion Seize Points)
public enum GodEventEffect { Envy, Suspicion, Seize, Points }

// 준비된 신 모드가 본편 규칙 위에 덧씌우는 규칙. 교단 행동은 본편 것을 그대로 쓰고, 다음만 바뀐다.
//  - 시작: 유년기 없이 성인의 몸(그릇)으로 바로 교단을 세운다. 기프티드·불운·연령 사망·회차 난이도·극단 엔딩은 없다.
//  - 신의 본성(바깥 노드 하나)을 고르면 교리 선포를 대신해 교단 행동이 열린다.
//  - 행동: 포교=이적, 설교=계시, 봉사=치유는 접신을 쓰고(모자라면 성공률 하락), 기도=교감은 접신을 채운다.
//          이적은 그릇(체력·정신)을 깎고, 욕구=향연은 그릇을 회복시킨다. 권능 트리의 성공률·증감 보정이 붙는다.
//  - 엔딩: 승천(중심 노드를 익힘) 6개, 패배 3개(그릇의 붕괴 / 잊힌 신 / 해산된 교단).
//  - 압박 두 축은 GodPressure. 신도·믿음이 0이면 잊힌 신, 명망이 0이거나 해산 명령이 내려지면 해산된 교단.
//  - 이벤트는 전용 목록(GodEvents)을 쓰고, 결과에 시기·의혹·압류·권능 포인트가 붙을 수 있다 (AfterEvent).
//  - 축성(봉헌 건축)은 GodBuildings. 행동 턴을 쓰고 건물이 스탯의 최소치를 지킨다.
public static class GodRules
{
    public const int UnlockEndings = 5;       // 사망·해체를 뺀 서로 다른 엔딩을 이만큼 보면 열린다
    public const int StartAgeYears = 20;
    public const int StartFollowers = 10, StartFollowerFaith = 300;
    public const float CommunionShortPenalty = 0.3f; // 접신이 모자라면 그 행동의 성공률 -30%p
    public const int CommuneRestore = 3;             // 교감 성공 시 접신 회복 (본편 기도 +1에 더해)
    public const int FeastRecovery = 3;              // 향연 성공 시 그릇(체력·정신) 회복
    public const int WonderVesselCost = 2;           // 이적이 그릇(체력)을 깎는 양 (정신은 1)
    public const float ConsecrateEnvy = 2f, ConsecrateSuspicion = 8f; // 기적으로 세운 건물은 눈에 띈다
    public const float EventPointsChance = 0.4f; // 압박 이벤트를 막아 낸 보상(Points)을 실제로 받을 확률 (전용 이벤트 밸런스)

    public static readonly Endings.Ending Vessel = new("god_vessel", EndingKind.Neutral, "god_vessel");
    public static readonly Endings.Ending Forgotten = new("god_forgotten", EndingKind.Neutral, "god_forgotten");
    public static readonly Endings.Ending Disbanded = new("god_disbanded", EndingKind.Neutral, "god_disbanded");

    public static readonly IReadOnlyList<Endings.Ending> AllEndings =
        GodEndings.AscensionIds.Select(id => new Endings.Ending(id, EndingKind.Neutral, id))
            .Concat(new[] { Vessel, Forgotten, Disbanded }).ToArray();

    public static Endings.Ending Find(string id) => AllEndings.FirstOrDefault(e => e.Id == id);
    public static bool IsAscension(Endings.Ending ending) => GodEndings.AscensionIds.Contains(ending.Id);

    // 해금 진행: 본 최종 스킬·극단 엔딩 수
    public static int UnlockProgress => Endings.All.Count(e => (e.Final != null || e.IsExtreme) && Endings.Seen(e.Id));
    public static bool Unlocked => UnlockProgress >= UnlockEndings;

    // 잠긴 버튼 제목(MenuLabel): 회색 취소선 글자 위로 엔딩 하나마다 금빛이 왼쪽부터 차오르고, 다 모이면 열린다.
    public static float LockedFill(int progress) => (float)Math.Min(progress, UnlockEndings - 1) / (UnlockEndings - 1);
    public static string LockedCount(int progress) => $"({Math.Min(progress, UnlockEndings)}/{UnlockEndings})";

    // 새 판: 성인 그릇의 시작 상태
    public static void Begin(int? seed = null)
    {
        GameSession.StartNew(seed);
        GameSession.SetGodMode(true);
        GodCounters.OnRunStart();
        GameSession.Age.Restore(StartAgeYears * GameClock.QuartersPerYear);
        var personal = GameSession.Personal;
        foreach (PersonalStatId id in Enum.GetValues(typeof(PersonalStatId))) personal[id].Set(StartStat(id));
        var cult = GameSession.Cult;
        cult[StatId.Communion].Set(CultStatus.CommunionMax);
        cult[StatId.Followers].Set(StartFollowers);
        cult[StatId.FollowerFaith].Set(StartFollowerFaith);
        GiftedV2Effects.OnRunStart();
    }

    static int StartStat(PersonalStatId id) => id switch
    {
        PersonalStatId.Miracle => PersonalStatus.MaxValue, // 신 본래의 힘
        PersonalStatId.Health or PersonalStatId.Mentality => 60,
        PersonalStatId.Piety or PersonalStatId.Repute or PersonalStatId.Wealth => 30,
        _ => 20,
    };

    public static GodAction ActionOf(CultAction action) => action switch
    {
        CultAction.Proselytize => GodAction.Wonder,
        CultAction.Preach => GodAction.Revelation,
        CultAction.Serve => GodAction.Healing,
        CultAction.Desire => GodAction.Feast,
        _ => GodAction.Commune,
    };

    public static int CommunionCost(CultAction action) => action switch
    {
        CultAction.Proselytize => 2,
        CultAction.Preach or CultAction.Serve => 1,
        _ => 0,
    };

    // 행동 성공률 보정 (본편이면 0)
    public static float ChanceBonus(CultAction action)
    {
        if (!GameSession.GodMode) return 0;
        var bonus = GameSession.God.Tree.Chance(ActionOf(action)) / 100f;
        if (GameSession.Cult[StatId.Communion].Value < CommunionCost(action)) bonus -= CommunionShortPenalty;
        return bonus;
    }

    // 행동 효과를 반영한 뒤: 접신 소모·회복, 그릇 소모·회복
    public static void AfterAction(CultAction action, bool success)
    {
        if (!GameSession.GodMode) return;
        var cult = GameSession.Cult;
        var personal = GameSession.Personal;
        var tree = GameSession.God.Tree;
        var save = Math.Min(tree.Total(GodEffect.Type.CommunionSave), GodEffects.MaxPercent) / 100f;
        cult.AddWithRemainder(StatId.Communion, -CommunionCost(action) * (1 - save));
        if (action == CultAction.Pray && success) cult[StatId.Communion].Add(CommuneRestore);
        if (action == CultAction.Proselytize)
        {
            GiftedEffects.Apply(personal, PersonalStatId.Health, -WonderVesselCost);
            GiftedEffects.Apply(personal, PersonalStatId.Mentality, -1);
        }
        GameSession.God.Pressure.AfterAction(action);
        GodCounters.OnAction(ActionOf(action), success);
        if (action == CultAction.Desire && success)
        {
            var recovery = FeastRecovery * (1 + tree.Total(GodEffect.Type.VesselRecovery) / 100f + GiftedV2GodHooks.VesselRecoveryBonus);
            personal.AddWithRemainder(PersonalStatId.Health, recovery);
            personal.AddWithRemainder(PersonalStatId.Mentality, recovery);
            GodCounters.OnVesselRecovery();
        }
    }

    // 스탯 변화에 거는 권능 보정 배율: 증가는 공격형(+%), 감소는 방어형(-%, 최대 90%). 본편이면 1.
    public static float Scale(PersonalStatId stat, int amount) =>
        GameSession.GodMode && amount != 0 ? GameSession.God.Tree.Adjust(stat, amount) / amount : 1f;

    public static float Scale(StatId stat, int amount) =>
        GameSession.GodMode && amount != 0 ? GameSession.God.Tree.Adjust(stat, amount) / amount : 1f;

    // 이벤트 선택지 성공률 감점 (본편이면 0)
    public static float EventPenalty =>
        GameSession.GodMode && GameSession.CultFounded ? GameSession.God.Pressure.EventPenalty(GameSession.Cult) : 0;

    // 곧바로 판이 끝나는 패배
    public static Endings.Ending ForcedEnding() => GodCounters.OnEnding(Forced());

    static Endings.Ending Forced()
    {
        if (!GameSession.CultFounded) return null;
        var personal = GameSession.Personal;
        var cult = GameSession.Cult;
        if ((personal[PersonalStatId.Health].Value <= 0 || personal[PersonalStatId.Mentality].Value <= 0) && !GiftedV2GodHooks.ResistVesselLoss(personal)) return Vessel;
        if (cult[StatId.Followers].Value <= 0 || cult[StatId.FollowerFaith].Value <= 0) return Forgotten;
        return Turns.CultDissolved || GameSession.God.Buildings.DissolutionOrdered ? Disbanded : null;
    }

    // 이벤트 결과의 신 모드 전용 효과를 반영하고 건물 하한을 지킨다
    public static void AfterEvent(Outcome outcome, string eventId = null)
    {
        if (!GameSession.GodMode) return;
        GodCounters.OnEvent(outcome);
        var god = GameSession.God;
        foreach (var (effect, amount) in outcome.GodEffects)
            switch (effect)
            {
                case GodEventEffect.Envy: god.Pressure.AddHeat(amount * GiftedV2GodHooks.EventHeatScale(eventId), 0); break;
                case GodEventEffect.Suspicion: god.Pressure.AddHeat(0, amount * GiftedV2GodHooks.EventHeatScale(eventId)); break;
                case GodEventEffect.Points:
                    if (GameSession.Rng.NextDouble() < EventPointsChance) god.Points.Defense(amount);
                    break;
                default:
                    for (int i = 0; i < amount; i++) god.Buildings.Seize(GameSession.Rng, god.Tree.Total(GodEffect.Type.SeizeGuard));
                    break;
            }
        god.Buildings.EnforceFloors(GameSession.Cult);
    }

    // 승천: 중심 노드를 익히면 먼저 완성한 두 핵의 조합으로 엔딩이 정해진다
    public static Endings.Ending DueEnding()
    {
        var tree = GameSession.God.Tree;
        var ending = tree.Ascended ? Find(tree.AscensionEndingId) : null;
        if (ending != null) GiftedV2GodHooks.BeforeAscension();
        return GodCounters.OnEnding(ending);
    }
}
