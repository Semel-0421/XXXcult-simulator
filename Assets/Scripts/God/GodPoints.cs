using System;

// 권능 포인트 (본편 교리 포인트에 해당). 쌓인 양을 기록해 두고, 권능 트리에 쓴 만큼 뺀 것이 남은 포인트다.
//  - 해가 바뀔 때마다 기본 2 + 신앙 비례 (신도 수 × 신도 신앙 비율 ÷ 25, 해마다 최대 5, faith 효과로 증가)
//  - 세속 규모 단계가 처음 오를 때마다 2 (같은 단계를 다시 올라도 또 주지 않는다)
//  - 압박 이벤트를 막아 낼 때마다 1
public class GodPoints
{
    public const int PerYear = 2, FaithPerPoint = 25, MaxFaithPerYear = 5, PerSecularStage = 2, PerDefense = 1;

    public int Earned { get; private set; }
    public int SecularStageAwarded { get; private set; }

    public static int FaithBonus(int followers, int followerFaith, float bonusPercent = 0, int faithMax = ClampedStat.DefaultMax)
    {
        if (followers <= 0 || followerFaith <= 0 || faithMax <= 0) return 0;
        var scale = followers * (float)followerFaith / faithMax;
        var points = scale / FaithPerPoint * (1 + bonusPercent / 100f);
        return Math.Clamp((int)points, 0, MaxFaithPerYear);
    }

    public int YearEnd(int followers, int followerFaith, float bonusPercent = 0)
    {
        var gained = PerYear + FaithBonus(followers, followerFaith, bonusPercent);
        Earned += gained;
        GodCounters.OnPoints(gained);
        return gained;
    }

    public int SecularStageReached(int stage)
    {
        if (stage <= SecularStageAwarded) return 0;
        var gained = (stage - SecularStageAwarded) * PerSecularStage;
        SecularStageAwarded = stage;
        Earned += gained;
        GodCounters.OnPoints(gained);
        return gained;
    }

    public void Defense(int amount = PerDefense) => Add(amount);
    public void Add(int amount) { Earned += Math.Max(0, amount); GodCounters.OnPoints(amount); }

    public int Available(GodTree tree) => Math.Max(0, Earned - tree.SpentPoints);

    public void Restore(int earned, int secularStageAwarded)
    {
        Earned = Math.Max(0, earned);
        SecularStageAwarded = Math.Max(0, secularStageAwarded);
    }
}

// 준비된 신 모드 한 판의 권능 상태 (트리 + 포인트)
public class GodState
{
    public GodTree Tree { get; } = new();
    public GodPoints Points { get; } = new();
    public GodBuildings Buildings { get; } = new();
    public GodPressure Pressure { get; }

    public AeonTree Aeons { get; } = new(); // 황금의 눈동자 모드의 아이온 트리 (신 모드에서는 비어 있음)
    public DemiurgeState Demiurge { get; } = new(); // 황금의 눈동자 모드의 데미우르고스 스탯표·재앙 카운터

    public GodState()
    {
        Pressure = new GodPressure(Tree, Buildings);
        Tree.Attach(Aeons);
        Aeons.Changed += _ => EyeCounters.OnLearn(Aeons);
    }
    public bool IntroShown { get; set; } // 권능 트리 확대 연출을 이번 판에 이미 보였는가 (저장하지 않음)

    public int Available => Points.Available(Tree);

    // 해가 바뀔 때: 건물 유지비, 포인트 적립(압박 단계 보너스·세속 단계 첫 도달 포함)과 해마다 효과
    public void YearEnd(CultStatus cult, PersonalStatus personal)
    {
        cult[StatId.Offerings].Add(GodBuildings.Tithe(cult[StatId.Followers].Value));
        Buildings.PayUpkeep(cult);
        Points.YearEnd(cult[StatId.Followers].Value, cult[StatId.FollowerFaith].Value, Tree.Total(GodEffect.Type.FaithPoints));
        Points.Add(Pressure.YearPoints(cult)); // 압박 단계 보너스
        Points.Add(Tree.BroadYearPoints);      // 넓게 찍은 단계 보너스
        Points.SecularStageReached(Pressure.Stage(global::Pressure.Earth, cult));
        Tree.ApplyYearly(personal, cult);
        GodCounters.OnYearEnd(Pressure);
    }

    // 분기마다: 압박이 교단을 공격한다. 지상 5단계면 건물을 압류하고, 압류할 건물이 없으면 일정 확률로 해산 명령. 건물 하한은 마지막에 지킨다.
    public void Quarter(CultStatus cult, System.Random rng = null)
    {
        rng ??= GameSession.Rng;
        Pressure.ApplyQuarter(cult);
        if (Pressure.Stage(global::Pressure.Earth, cult) >= GodPressure.MaxStage && !Buildings.Seize(rng, Tree.Total(GodEffect.Type.SeizeGuard))
            && rng.NextDouble() < GodBuildings.DissolutionChance(Tree.BroadRings))
            Buildings.OrderDissolution();
        Buildings.EnforceFloors(cult);
    }
}
