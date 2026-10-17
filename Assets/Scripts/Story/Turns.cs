using System.Collections.Generic;

// 한 턴의 규칙(판정·효과·카운트·시간 경과)을 화면 연출과 분리해 둔다.
// 흐름(LifeFlow/CultFlow/QuestionRunner)과 시뮬레이션 테스트가 같은 순서로 쓰도록 한곳에 모은다.
public static class Turns
{
    // 생활 행동 1턴. 성공 여부를 돌려준다.
    public static bool Life(LifeAction action)
    {
        var success = LifeActions.Roll(GameSession.Personal, action, GameSession.Rng);
        LifeActions.Apply(GameSession.Personal, action, success);
        GameSession.Count($"act_{action}");
        GameSession.Count("life_acts");
        GameSession.Count(success ? "act_ok" : "act_fail");
        GameSession.AdvanceTurn();
        return success;
    }

    // 교단 행동 1턴(1분기): 효과 → 사회적 시선 견제/지원 → 쌓인 불운만큼 쇠퇴 → 시간 경과.
    public static bool Cult(CultAction action)
    {
        var cult = GameSession.Cult;
        var success = CultActions.Roll(cult, GameSession.Personal.Talents, action, GameSession.Rng, GodRules.ChanceBonus(action));
        CultActions.Apply(cult, GameSession.Personal, action, success);
        GodRules.AfterAction(action, success);
        GameSession.Count($"act_{CultActions.Key(action)}");
        GameSession.Count("cult_acts");
        GameSession.Count(success ? "act_ok" : "act_fail");
        SocialStanding.ApplyTurn(cult);
        Misfortune.ApplyTurn(cult, GameSession.Misfortune, Misfortune.TotalResistance(cult));
        GameSession.AdvanceTurn();
        return success;
    }

    // 명망이 바닥나 국가의 명령으로 해체될 상태인가
    public static bool CultDissolved => SocialStanding.Of(GameSession.Cult) == Standing.Dissolved;

    public static LifeEvent NextLifeEvent() =>
        LifeEvent.Pick(LifeEvents.All, GameSession.Age.Years, GameSession.UsedEvents, GameSession.Rng);

    // 이번 판의 교단 이벤트 목록 (준비된 신 모드는 전용 이벤트)
    public static IReadOnlyList<CultEvent> CultPool => GameSession.GodMode ? GodEvents.All : CultEvents.All;

    // 교단 행동 뒤 이어질 이벤트: 조건 → 부정적 → 긍정(무드) → 확률 순. 없으면 null. 부정적 이벤트면 불운이 한 겹 쌓인다.
    // 준비된 신 모드: 압박 이벤트(신계·지상, 압박 단계에 비례한 확률) → 확률 순. 불운은 없다.
    public static CultEvent NextCultEvent()
    {
        var cult = GameSession.Cult;
        var used = GameSession.UsedEvents;
        var rng = GameSession.Rng;
        if (GameSession.GodMode)
            return CultEvent.RollNegative(GodEvents.All, cult, used, rng, GameSession.God.Pressure.EventChance(cult))
                   ?? RollAdult(cult, used, rng)
                   ?? CultEvent.RollRandom(GodEvents.All, cult, used, rng);
        var next = CultEvent.FindTriggered(CultEvents.All, cult, used)
                   ?? CultEvent.RollNegative(CultEvents.All, cult, used, rng, GameSession.CultTime.Years + 1)
                   ?? CultEvent.RollMood(CultEvents.All, GameSession.Personal, used, rng)
                   ?? RollAdult(cult, used, rng)
                   ?? CultEvent.RollRandom(CultEvents.All, cult, used, rng);
        if (next != null && next.IsNegative) GameSession.AddMisfortune();
        return next;
    }

    // 성인 이벤트: 꺼져 있으면 난수를 쓰지 않아 기존 판 진행이 그대로다
    static CultEvent RollAdult(CultStatus cult, ISet<string> used, System.Random rng) =>
        AdultGate.Active && rng.NextDouble() < AdultGate.EventChance ? CultEvent.RollRandom(AdultEvents.Pool(GameSession.GodMode), cult, used, rng) : null;

    // 고른 선택지의 성공/실패를 판정하고 결과의 효과(기프티드 보정 포함)와 기프티드 지급을 반영한다. 시간은 흐르지 않는다.
    public static Outcome Answer(Choice choice)
    {
        var success = choice.Roll(GameSession.Rng, GameSession.Personal, GodRules.EventPenalty); // 교단 이벤트는 개인 스탯 보정이 붙는다
        var outcome = choice.Resolve(success);
        GameSession.Count("events");
        GameSession.Count(success ? "choice_ok" : "choice_fail");
        foreach (var effect in outcome.Effects)
            GiftedEffects.Apply(GameSession.Personal, effect.Stat, effect.Amount);
        foreach (var effect in outcome.CultEffects)
            GiftedEffects.Apply(GameSession.Cult, GameSession.Personal.Talents, effect.Stat, effect.Amount);
        foreach (var id in outcome.Grants)
            GiftedGranter.Grant(GameSession.Personal.Talents, id);
        GodRules.AfterEvent(outcome);
        return outcome;
    }

    // 준비된 신 모드의 축성 1턴: 기적으로 건물을 세우거나 올린다. 지을 수 없으면 턴을 쓰지 않고 false.
    public static bool Consecrate(GodBuildingType type)
    {
        var cult = GameSession.Cult;
        var god = GameSession.God;
        if (!god.Buildings.Build(type, god.Tree, cult)) return false;
        god.Pressure.AddHeat(GodRules.ConsecrateEnvy, GodRules.ConsecrateSuspicion);
        GameSession.Count("act_Consecrate");
        GameSession.Count("cult_acts");
        GameSession.Count("act_ok");
        SocialStanding.ApplyTurn(cult);
        GameSession.AdvanceTurn();
        return true;
    }
}
