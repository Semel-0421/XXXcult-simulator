using System;
using System.Collections.Generic;
using System.Linq;

// 권능 트리 진행 상태. 바깥 고리 노드는 언제든 익힐 수 있고, 안쪽 노드는 바로 바깥의 두 노드를 모두 익혀야 열린다.
// 시작할 때 바깥 노드 하나를 "신의 본성"으로 무료로 고른다(기프티드 대체). 핵 두 개를 완성하면 중심(승천)이 열리고,
// 먼저 완성한 두 핵의 조합으로 승천 엔딩이 정해진다.
public class GodTree
{
    readonly HashSet<string> learned = new();
    readonly List<string> coreOrder = new();
    Dictionary<(GodEffect.Type, string), int> totals;

    public event Action<GodSkill> Changed;

    public string NatureId { get; private set; }
    public bool HasNature => NatureId != null;
    public IEnumerable<string> Learned => learned;
    public IReadOnlyList<string> CoreOrder => coreOrder;

    public bool IsLearned(GodSkill skill) => skill != null && learned.Contains(skill.Id);
    public bool IsNature(GodSkill skill) => skill != null && skill.Id == NatureId;
    public int LearnedChildren(GodSkill skill) => skill.Children.Count(IsLearned);

    // 승천은 핵 두 개, 바깥 고리는 조건 없음, 나머지는 바로 바깥 두 노드 모두
    public bool PrerequisitesMet(GodSkill skill) =>
        skill.IsCenter ? coreOrder.Count >= 2 : skill.IsOuter || skill.Children.All(IsLearned);

    public bool CanChooseNature(GodSkill skill) => !HasNature && skill != null && skill.IsOuter;

    public bool ChooseNature(GodSkill skill)
    {
        if (!CanChooseNature(skill)) return false;
        NatureId = skill.Id;
        Add(skill);
        return true;
    }

    public bool CanLearn(GodSkill skill, int availablePoints) =>
        skill != null && HasNature && !IsLearned(skill) && PrerequisitesMet(skill) && availablePoints >= skill.Cost;

    public bool Learn(GodSkill skill, int availablePoints)
    {
        if (!CanLearn(skill, availablePoints)) return false;
        Add(skill);
        return true;
    }

    void Add(GodSkill skill)
    {
        learned.Add(skill.Id);
        if (skill.IsCore) coreOrder.Add(skill.Id);
        totals = null;
        Changed?.Invoke(skill);
    }

    // 본성은 무료라 비용에 넣지 않는다
    public int SpentPoints => learned.Where(id => id != NatureId).Sum(id => GodSkillCatalog.Find(id)?.Cost ?? 0);

    public bool AscensionOpen => coreOrder.Count >= 2;
    public bool Ascended => learned.Contains(GodSkill.AscensionId);

    // 지금까지 이른 가장 안쪽 단계 (0: 아무것도 없음, 1 그릇 ~ 5 신, 6 승천)
    public int ReachedStage => learned.Count == 0 ? 0 : learned.Max(id => GodSkillCatalog.Find(id)?.Stage ?? 0);

    // 먼저 완성한 두 핵. 승천 엔딩을 정한다 (핵이 둘 미만이면 null)
    public (GodBranch first, GodBranch second)? FirstCores =>
        coreOrder.Count < 2 ? null : (GodSkillCatalog.Find(coreOrder[0]).Branch, GodSkillCatalog.Find(coreOrder[1]).Branch);

    public string AscensionEndingId => FirstCores is { } pair ? GodEndings.IdFor(pair.first, pair.second) : null;

    // ---- 단계 보너스 (넓게 찍은 보상) ----

    // 네 가지 모두가 그 고리 노드의 절반 이상을 익힌 고리 수. 고리마다 안정성(저항·손실 방어)을 크게, 승천 속도(포인트)를 조금 준다.
    public const int BroadResist = 10, BroadGuard = 8, BroadRingsPerPoint = 2;

    public int BroadRings => Enumerable.Range(1, 5).Count(ring =>
        GodSkillCatalog.Cores.All(core => IsBroad(core.Branch, ring)));

    bool IsBroad(GodBranch branch, int ring)
    {
        var nodes = GodSkillCatalog.All.Where(s => s.Branch == branch && s.Ring == ring).ToList();
        return nodes.Count(IsLearned) * 2 >= nodes.Count;
    }

    // 해마다 더 받는 권능 포인트
    public int BroadYearPoints => BroadRings / BroadRingsPerPoint;

    // ---- 효과 합계 ----

    public int Total(GodEffect.Type type, string key = "") => Totals().TryGetValue((type, key), out var sum) ? sum : 0;

    public int Gain(StatId stat) => Total(GodEffect.Type.Gain, $"c:{stat}");
    public int Gain(PersonalStatId stat) => Total(GodEffect.Type.Gain, $"p:{stat}");
    public int Guard(StatId stat) => Total(GodEffect.Type.Guard, $"c:{stat}");
    public int Guard(PersonalStatId stat) => Total(GodEffect.Type.Guard, $"p:{stat}");
    public int Resist(Pressure pressure) => Total(GodEffect.Type.Resist, pressure.ToString());
    public int Chance(GodAction action) => Total(GodEffect.Type.Chance, action.ToString()) + Total(GodEffect.Type.Chance, GodEffect.AllActions);

    // 스탯 변화량에 공격형(증가)·방어형(감소) 보정을 건다
    public float Adjust(StatId stat, int amount) => GodEffects.Adjust(amount, Gain(stat), Guard(stat));
    public float Adjust(PersonalStatId stat, int amount) => GodEffects.Adjust(amount, Gain(stat), Guard(stat));

    // 해가 바뀔 때 해마다 효과를 적용한다
    public void ApplyYearly(PersonalStatus personal, CultStatus cult)
    {
        foreach (var skill in learned.Select(GodSkillCatalog.Find).Where(s => s != null))
            foreach (var effect in skill.Effects)
            {
                if (effect.Kind != GodEffect.Type.Yearly) continue;
                if (effect.PersonalStat is { } p) personal[p].Add(skill.AmountOf(effect));
                else if (effect.CultStat is { } c) cult.AddWithRemainder(c, skill.AmountOf(effect));
            }
    }

    Dictionary<(GodEffect.Type, string), int> Totals()
    {
        if (totals != null) return totals;
        totals = new Dictionary<(GodEffect.Type, string), int>();
        foreach (var skill in learned.Select(GodSkillCatalog.Find).Where(s => s != null))
            foreach (var effect in skill.Effects)
            {
                var key = (effect.Kind, KeyOf(effect));
                totals[key] = (totals.TryGetValue(key, out var sum) ? sum : 0) + skill.AmountOf(effect);
            }
        var broad = BroadRings;
        if (broad > 0)
        {
            void Add(GodEffect.Type type, string key, int amount) => totals[(type, key)] = (totals.TryGetValue((type, key), out var sum) ? sum : 0) + amount;
            foreach (var pressure in new[] { "Heaven", "Earth" }) Add(GodEffect.Type.Resist, pressure, BroadResist * broad);
            foreach (var stat in new[] { StatId.Followers, StatId.FollowerFaith, StatId.Reputation }) Add(GodEffect.Type.Guard, $"c:{stat}", BroadGuard * broad);
        }
        return totals;
    }

    static string KeyOf(GodEffect effect) => effect.Kind switch
    {
        GodEffect.Type.Gain or GodEffect.Type.Guard or GodEffect.Type.Yearly =>
            effect.CultStat is { } c ? $"c:{c}" : $"p:{effect.PersonalStat}",
        GodEffect.Type.Chance => effect.Action,
        GodEffect.Type.Resist => effect.PressureKind.ToString(),
        _ => "",
    };

    // ---- 저장 ----

    public void Restore(IEnumerable<string> learnedIds, string natureId, IEnumerable<string> cores)
    {
        learned.Clear();
        coreOrder.Clear();
        foreach (var id in learnedIds ?? Enumerable.Empty<string>())
            if (GodSkillCatalog.Find(id) != null) learned.Add(id);
        foreach (var id in cores ?? Enumerable.Empty<string>())
            if (learned.Contains(id) && GodSkillCatalog.Find(id).IsCore && !coreOrder.Contains(id)) coreOrder.Add(id);
        NatureId = string.IsNullOrEmpty(natureId) || !learned.Contains(natureId) ? null : natureId;
        totals = null;
    }
}

public static class GodEffects
{
    public const float MaxPercent = 90f; // 방어·저항은 90%까지만 막는다

    // 늘어나는 변화는 증가 보정만, 줄어드는 변화는 감소 보정만 받는다
    public static float Adjust(int amount, float gainPercent, float guardPercent) =>
        amount > 0 ? amount * (1 + gainPercent / 100f)
        : amount < 0 ? amount * (1 - Math.Min(guardPercent, MaxPercent) / 100f)
        : 0;

    // 압박 가산치 상승분을 저항만큼 줄인다 (내려가는 쪽은 그대로)
    public static float Resisted(int amount, float resistPercent) =>
        amount > 0 ? amount * (1 - Math.Min(resistPercent, MaxPercent) / 100f) : amount;
}

// 승천 엔딩 id: 먼저 완성한 신격 핵 + 인격 핵의 조합 4가지, 같은 종류 핵 두 개는 히든 엔딩 2가지
public static class GodEndings
{
    public const string DivinePair = "god_divine"; // 신격 핵 둘 (기적 + 예언)
    public const string HumanPair = "god_human";   // 인격 핵 둘 (자비 + 욕망)

    public static string IdFor(GodBranch first, GodBranch second)
    {
        bool firstDivine = first is GodBranch.Miracle or GodBranch.Prophecy;
        bool secondDivine = second is GodBranch.Miracle or GodBranch.Prophecy;
        if (firstDivine && secondDivine) return DivinePair;
        if (!firstDivine && !secondDivine) return HumanPair;
        var divine = firstDivine ? first : second;
        var human = firstDivine ? second : first;
        return $"god_{divine.ToString().ToLowerInvariant()}_{human.ToString().ToLowerInvariant()}";
    }

    public static readonly string[] AscensionIds =
    {
        "god_miracle_mercy", "god_miracle_desire", "god_prophecy_mercy", "god_prophecy_desire", DivinePair, HumanPair,
    };
}
