using System;
using System.Collections.Generic;
using System.Linq;

// 봉헌 건축(축성, 기획서 8장). 기적으로 건물을 세워 위세(권위)를 증명하고, 지킬 스탯의 최소치(방어 하한)를 고정한다.
//  - 축성은 행동 턴을 쓴다: 봉헌금 + 접신 2. 레벨은 권능 트리에서 도달한 고리 단계까지 올릴 수 있다.
//  - 건물 종류는 권능 트리 기적 → 이적 → 재력 구역 노드로 연다 (바깥은 작은 건물, 안쪽은 큰 건물).
//  - 해마다 유지비를 내고, 못 내는 건물은 한 레벨 내려간다. 건물 가치는 세속 규모(지상 압박)에 더해진다.
//  - 지상 압박의 압류는 건물을 한 레벨 내린다. 지상 5단계에서 압류할 건물이 없으면 분기마다 일정 확률로 해산 명령.
public enum GodBuildingType { Statue, Temple, Library, Village, Shelter }

public class GodBuildings
{
    public const int MaxLevel = 5, CommunionCost = 2, AuthorityPerLevel = 5;
    public const float DissolutionChancePerQuarter = 0.12f; // 지상 5단계에서 압류할 건물이 없을 때 해산 명령이 내려질 확률 (평균 2년 유예)
    public const float BroadDissolutionCut = 0.15f;          // 단계 보너스 고리 하나마다 해산 확률을 이만큼(비율) 줄인다: 넓게 찍은 교단이 가장 버틴다

    public static float DissolutionChance(int broadRings) => DissolutionChancePerQuarter * System.Math.Max(0f, 1f - BroadDissolutionCut * broadRings);
    static readonly int[] Cost = { 50, 100, 200, 350, 500 };
    static readonly int[] FloorPercent = { 5, 10, 15, 22, 30 }; // 최대치 대비 하한 (최대 30%)
    static readonly int[] Upkeep = { 5, 10, 20, 35, 50 };
    static readonly int[] FollowerFloor = { 10, 20, 40, 70, 100 }; // 신도 수는 최대치가 없어 고정 인원

    public static readonly GodBuildingType[] All = (GodBuildingType[])Enum.GetValues(typeof(GodBuildingType));

    // 건물 종류를 여는 권능 노드
    public static readonly IReadOnlyDictionary<string, GodBuildingType> UnlockNodes = new Dictionary<string, GodBuildingType>
    {
        ["l_wealth_atk2"] = GodBuildingType.Statue,
        ["l_wealth_def2"] = GodBuildingType.Shelter,
        ["t_wealth_atk"] = GodBuildingType.Village,
        ["t_wealth_def"] = GodBuildingType.Library,
        ["s_wealth"] = GodBuildingType.Temple,
    };

    readonly int[] levels = new int[All.Length];

    public bool DissolutionOrdered { get; private set; }
    public IReadOnlyList<int> Levels => levels;

    public int LevelOf(GodBuildingType type) => levels[(int)type];

    public static StatId Guards(GodBuildingType type) => type switch
    {
        GodBuildingType.Statue => StatId.Authority,
        GodBuildingType.Temple => StatId.FollowerFaith,
        GodBuildingType.Library => StatId.Doctrine,
        GodBuildingType.Village => StatId.Followers,
        _ => StatId.Reputation,
    };

    public static int Floor(GodBuildingType type, int level) => level <= 0 ? 0
        : (int)System.Math.Round((type == GodBuildingType.Village ? FollowerFloor[level - 1]
            : ClampedStat.DefaultMax * FloorPercent[level - 1] / 100) * GiftedV2GodHooks.BuildingEffectScale(type));

    public static int CostOf(int level) => GiftedV2GodHooks.BuildingCost(Cost[level - 1]);
    public static int UpkeepOf(int level) => level <= 0 ? 0 : Upkeep[level - 1];

    public static bool IsUnlocked(GodBuildingType type, GodTree tree) =>
        UnlockNodes.Any(pair => pair.Value == type && tree.IsLearned(GodSkillCatalog.Find(pair.Key)));

    // 왜 지을 수 없는지 (지을 수 있으면 null): locked / max / stage / offerings / communion
    public string Blocker(GodBuildingType type, GodTree tree, CultStatus cult)
    {
        var next = LevelOf(type) + 1;
        if (!IsUnlocked(type, tree)) return "locked";
        if (next > MaxLevel) return "max";
        if (tree.ReachedStage < next) return "stage";
        if (cult[StatId.Offerings].Value < CostOf(next)) return "offerings";
        if (cult[StatId.Communion].Value < CommunionCost) return "communion";
        return null;
    }

    public bool Build(GodBuildingType type, GodTree tree, CultStatus cult)
    {
        if (Blocker(type, tree, cult) != null) return false;
        var level = ++levels[(int)type];
        cult[StatId.Offerings].Add(-CostOf(level));
        cult[StatId.Communion].Add(-CommunionCost);
        cult[StatId.Authority].Add(AuthorityPerLevel * level);
        EnforceFloors(cult);
        GodCounters.OnBuild(type);
        return true;
    }

    public void EnforceFloors(CultStatus cult)
    {
        foreach (var type in All)
        {
            var floor = Floor(type, LevelOf(type));
            var stat = cult[Guards(type)];
            if (stat.Value < floor) stat.Set(floor);
        }
    }

    // 해마다: 목록 순서대로 유지비를 내고, 못 내는 건물은 한 레벨 내린다
    public void PayUpkeep(CultStatus cult)
    {
        foreach (var type in All)
        {
            var upkeep = UpkeepOf(LevelOf(type));
            if (upkeep == 0) continue;
            if (cult[StatId.Offerings].Value >= upkeep) cult[StatId.Offerings].Add(-upkeep);
            else levels[(int)type]--;
        }
    }

    // 압류: 가장 높은 건물을 한 레벨 내린다. 방어 확률에 걸리면 막는다. 압류할 건물이 없으면 false.
    public bool Seize(Random rng, int guardPercent)
    {
        var target = All.Where(t => LevelOf(t) > 0).OrderByDescending(LevelOf).FirstOrDefault();
        if (LevelOf(target) <= 0) return false;
        if (rng.NextDouble() * 100 < guardPercent) return true;
        levels[(int)target]--;
        GodCounters.OnSeized();
        return true;
    }

    public void OrderDissolution() => DissolutionOrdered = true;

    // 세속 규모에 더하는 건물 가치 (들인 축성 비용 합)
    public int Value => All.Sum(type => Enumerable.Range(1, LevelOf(type)).Sum(l => Cost[l - 1]));

    public void Restore(IList<int> saved, bool dissolution)
    {
        for (int i = 0; i < levels.Length; i++)
            levels[i] = saved != null && i < saved.Count ? Math.Clamp(saved[i], 0, MaxLevel) : 0;
        DissolutionOrdered = dissolution;
    }
}
