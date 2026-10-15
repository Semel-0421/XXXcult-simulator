using System;
using System.Linq;

// 준비된 신 모드의 압박 두 축 (기획서 6장). 수치는 GodSimulation(봇 100명 × 10판)으로 맞췄다:
// 핵 루트 봇 패배 약 21%, 승천 약 16년 / 같은 계열 핵 둘(히든) 패배 약 37% / 넓게 찍는 분산 봇 패배 약 42%, 승천 약 28년.
//  - 신계: 신앙 규모(신도 수 × 신도 믿음 비율 + 교리)와 시기(Envy)로 단계가 오른다. 신도 수·신도 믿음·교리를 깎는다.
//  - 지상: 세속 규모(봉헌금 + 권위)와 의혹(Suspicion)으로 단계가 오른다. 명망·봉헌금을 깎는다.
//  - 신의 힘: 익힌 노드마다 두 축 규모가 함께 오른다. 승천에 가까울수록 압박은 피할 수 없이 커진다.
//  - 저항: 권능 트리의 Resist 효과 + 깊이 보너스. 신격 가지(기적·예언)는 신계를, 인격 가지(자비·욕망)는 지상을 막고,
//    안쪽 고리일수록 크다. 저항은 피해, 가산치 상승, 이벤트 감점을 함께 줄인다.
//  - 히든 규칙: 신격 깊이는 지상을, 인격 깊이는 신계를 키운다. 신격+인격 조합은 서로 상쇄되지만,
//    같은 계열 핵 둘을 노리면 막지 못하는 축이 끝까지 커진다.
//  - 이벤트: 압박 단계만큼 선택지 성공률이 떨어진다 (EventPenalty).
//  - 가산치: 이적·향연은 의혹을, 계시·이적은 시기를 쌓는다. 분기마다 조금씩 가라앉는다.
//  - 보상: 해마다 두 축 단계 합 × PointsPerStage 만큼 권능 포인트를 더 받는다 (압박이 클수록 승천이 빨라진다).
public class GodPressure
{
    public const int MaxStage = 5;
    static readonly int[] HeavenSteps = { 30, 70, 130, 210, 320 };
    static readonly int[] EarthSteps = { 120, 280, 480, 720, 1000 };
    public const int HeavenPerNode = 1, EarthPerNode = 4;   // 익힌 노드 하나당 규모
    public const int HeavenPerDepth = 1, EarthPerDepth = 3; // 반대 계열 깊이 보너스 1%당 규모 (히든 규칙)
    public const float Decay = 0.94f;                       // 분기마다 가산치가 남는 비율
    public const float PointsPerStage = 0.7f;               // 해마다 (신계 + 지상 단계) × 이 값의 권능 포인트 (내림)
    public const float EventPenaltyPerStage = 0.03f;        // 단계당 이벤트 성공률 감점 (저항만큼 줄어듦)
    const float HitDivisor = 3.6f;                          // 단계별 피해 배율 = 단계² ÷ 이 값

    readonly GodTree tree;
    public GodPressure(GodTree tree) => this.tree = tree;

    public float Envy { get; private set; }
    public float Suspicion { get; private set; }

    // ---- 단계 ----

    public static int HeavenScale(CultStatus cult) =>
        cult[StatId.Followers].Value * cult[StatId.FollowerFaith].Value / ClampedStat.DefaultMax + cult[StatId.Doctrine].Value / 4;

    public static int EarthScale(CultStatus cult) => cult[StatId.Offerings].Value + cult[StatId.Authority].Value;

    public int Stage(Pressure pressure, CultStatus cult)
    {
        var nodes = tree.Learned.Count();
        return pressure == Pressure.Heaven
            ? StageOf(HeavenScale(cult) + nodes * HeavenPerNode + Depth(Pressure.Earth) * HeavenPerDepth + Envy, HeavenSteps)
            : StageOf(EarthScale(cult) + nodes * EarthPerNode + Depth(Pressure.Heaven) * EarthPerDepth + Suspicion, EarthSteps);
    }

    static int StageOf(float scale, int[] steps) => steps.Count(step => scale >= step);

    // ---- 저항 ----

    // 깊이 보너스: 고리 2 = 2%, 3 = 4%, 4 = 8%, 핵 = 20% (가지 하나를 끝까지 올리면 68%)
    public static int DepthBonus(GodSkill skill) => skill.Ring switch { 2 => 2, 3 => 4, 4 => 8, 5 => 20, _ => 0 };

    public static Pressure Guards(GodSkill skill) => skill.IsDivine ? Pressure.Heaven : Pressure.Earth;

    // 이 축을 막는 계열로 쌓은 깊이 보너스 합
    int Depth(Pressure guarded) => tree.Learned.Select(GodSkillCatalog.Find)
        .Where(s => s != null && s.Branch != GodBranch.None && Guards(s) == guarded).Sum(DepthBonus);

    // 0~0.9
    public float Resistance(Pressure pressure) => Math.Min(tree.Resist(pressure) + Depth(pressure), GodEffects.MaxPercent) / 100f;

    // 저항을 뺀, 이 축이 실제로 들어오는 비율
    float Exposure(Pressure pressure) => 1 - Resistance(pressure);

    // ---- 효과 ----

    public float EventPenalty(CultStatus cult) => EventPenaltyPerStage *
        (Stage(Pressure.Heaven, cult) * Exposure(Pressure.Heaven) + Stage(Pressure.Earth, cult) * Exposure(Pressure.Earth));

    // 행동이 쌓는 가산치 (오르는 쪽만 저항을 받는다)
    public void AfterAction(CultAction action)
    {
        var (envy, suspicion) = action switch
        {
            CultAction.Proselytize => (4f, 10f), // 공개 기적
            CultAction.Preach => (6f, 0f),
            CultAction.Desire => (0f, 6f),
            CultAction.Serve => (0f, -4f),       // 치유는 여론을 달랜다
            _ => (-3f, 0f),                      // 교감: 조용히 지낸다
        };
        Envy = Math.Max(0, Envy + (envy > 0 ? envy * Exposure(Pressure.Heaven) : envy));
        Suspicion = Math.Max(0, Suspicion + (suspicion > 0 ? suspicion * Exposure(Pressure.Earth) : suspicion));
    }

    // 분기마다: 단계만큼 공격하고 가산치가 가라앉는다. 피해는 단계의 제곱에 비례해 막바지에 몰아친다.
    public void ApplyQuarter(CultStatus cult)
    {
        var heaven = Stage(Pressure.Heaven, cult);
        var earth = Stage(Pressure.Earth, cult);
        var heavenHit = heaven * heaven / HitDivisor * Exposure(Pressure.Heaven);
        var earthHit = earth * earth / HitDivisor * Exposure(Pressure.Earth);
        if (heaven > 0)
        {
            cult.AddWithRemainder(StatId.Followers, -cult[StatId.Followers].Value * 0.024f * heavenHit);
            cult.AddWithRemainder(StatId.FollowerFaith, -12f * heavenHit);
            if (heaven >= 2) cult.AddWithRemainder(StatId.Doctrine, -6f * heavenHit);
        }
        if (earth > 0)
        {
            cult.AddWithRemainder(StatId.Reputation, -12f * earthHit);
            if (earth >= 3) cult.AddWithRemainder(StatId.Offerings, -10f * earthHit); // 봉헌금 압수
        }
        Envy *= Decay;
        Suspicion *= Decay;
    }

    public int YearPoints(CultStatus cult) => (int)((Stage(Pressure.Heaven, cult) + Stage(Pressure.Earth, cult)) * PointsPerStage);

    public void Restore(float envy, float suspicion)
    {
        Envy = Math.Max(0, envy);
        Suspicion = Math.Max(0, suspicion);
    }
}
